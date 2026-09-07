namespace MandelbrotViewer;

/// <summary>
/// Video zoom MP4: interpola dalla vista corrente all'insieme completo (scala in
/// logaritmo, centro lineare, iterazioni auto per frame) e rende ogni frame col
/// motore attivo a risoluzione vista e AA1x. Codifica con ffmpeg se presente
/// (altrimenti resta la sequenza PNG). Annullabile.
/// </summary>
public partial class ZoomVideoForm : Form
{
    /// <summary>Tetto campioni totali per frame (come ExportForm): oltre, l'AA scende.</summary>
    private const long MaxSamples = 134_217_728; // 128 MPixel

    private readonly double _endCx, _endCy, _endScale;
    private readonly Palette _palette;
    private readonly int _aa;
    private readonly bool _useCuda;
    private readonly bool _useDirectX;
    private readonly bool _useDouble;
    private readonly bool _julia;
    private readonly double _jcx, _jcy;
    private readonly int _viewW, _viewH;
    private CancellationTokenSource? _cts;
    private bool _rendering;
    private string _resultPath = ""; // MP4 creato o cartella PNG (per Apri)

    public ZoomVideoForm(double endCx, double endCy, double endScale, Palette palette, int aa,
        RenderEngine engine, bool useCuda, bool useDouble, Size viewSize,
        bool julia, double juliaCx, double juliaCy)
    {
        InitializeComponent();
        Program.ApplyIcon(this);

        _endCx = endCx;
        _endCy = endCy;
        _endScale = endScale;
        _palette = palette;
        _aa = Math.Max(1, aa);
        _useCuda = useCuda;
        _useDirectX = engine == RenderEngine.DirectX && DxMandelbrot.IsReady;
        _useDouble = useDouble;
        _julia = julia;
        _jcx = juliaCx;
        _jcy = juliaCy;
        _viewW = Math.Max(320, viewSize.Width);
        _viewH = Math.Max(240, viewSize.Height);

        UpdateInfo();
    }

    /// <summary>AA scelto nel dialog (Come vista = quello attivo) o della vista.</summary>
    private int SelectedAa() =>
        cmbAAVid.SelectedIndex <= 0 ? _aa : 1 << (cmbAAVid.SelectedIndex - 1);

    private void UpdateInfo()
    {
        string engineLabel = _useCuda ? $"CUDA {(_useDouble ? "64-bit" : "32-bit")}"
            : _useDirectX ? "DirectX" : "CPU";
        string mode = _julia ? "Julia" : "Mandelbrot";
        int reqAa = SelectedAa();
        int effAa = EffectiveAa();
        string aaNote = effAa < reqAa ? $" (AA ridotto da {reqAa}x: oltre il tetto)" : "";
        lblInfo.Text = $"Dalla vista corrente all'insieme ({_viewW}×{_viewH} AA{effAa}x{aaNote}, {mode}, " +
            $"{engineLabel}, iter auto): ffmpeg " +
            (FindFfmpeg() != null ? "trovato → MP4 diretto." : "assente → resta la sequenza PNG.");
    }

    private void CmbAAVid_Changed(object? sender, EventArgs e) => UpdateInfo();

    /// <summary>AA effettivo: quello selezionato, ridotto a potenze di 2 finché i
    /// campioni totali del frame rientrano nel tetto.</summary>
    private int EffectiveAa()
    {
        int a = SelectedAa();
        while ((long)_viewW * a * _viewH * a > MaxSamples && a > 1)
            a /= 2;
        return a;
    }

    /// <summary>Percorso di ffmpeg (PATH) o null se assente.</summary>
    internal static string? FindFfmpeg()
    {
        foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
        {
            try
            {
                string cand = Path.Combine(dir.Trim(), "ffmpeg.exe");
                if (File.Exists(cand)) return cand;
            }
            catch
            {
                // Voci di PATH non valide: ignora e continua.
            }
        }
        return null;
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_rendering) { _cts?.Cancel(); return; }
        int frames = int.Parse((string)cmbFrames.SelectedItem!);
        int fps = int.Parse((string)cmbFps.SelectedItem!);
        int effAa = EffectiveAa();

        if (MandelbrotForm.StartScale / _endScale < 2.0)
        {
            MessageBox.Show(this,
                "Sei già all'insieme completo: inquadra prima una zona per generare il video.",
                "Video zoom", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "MP4 (*.mp4)|*.mp4",
            FileName = "mandelbrot-zoom.mp4",
            DefaultExt = "mp4",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _rendering = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var progress = new Progress<int>(i =>
        {
            progressBar.Value = Math.Clamp((i + 1) * 100 / frames, 0, 100);
            lblResult.Text = $"Frame {i + 1}/{frames}…";
        });
        btnStart.Text = "Annulla";
        btnClose.Enabled = false;
        btnOpen.Enabled = false;
        _resultPath = "";

        // Direzione: dalla vista corrente all'insieme (il caso "già all'insieme"
        // è bloccato sopra con errore).
        double startCx = _endCx, startCy = _endCy, startScale = _endScale;
        string tmpDir = Path.Combine(Path.GetTempPath(),
            "MandelC#-video-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tmpDir);
            await Task.Run(() => RenderFrames(tmpDir, frames, startCx, startCy, startScale, effAa, progress, token), token);
            token.ThrowIfCancellationRequested();

            string? ffmpeg = FindFfmpeg();
            if (ffmpeg == null)
            {
                _resultPath = tmpDir;
                btnOpen.Enabled = true;
                lblResult.Text = $"ffmpeg assente: sequenza PNG in {tmpDir}";
                tmpDir = ""; // non cancellare: è il risultato
                return;
            }
            lblResult.Text = "Codifica MP4…";
            await Task.Run(() => RunFfmpeg(ffmpeg, tmpDir, frames, fps, dlg.FileName, token), token);
            _resultPath = dlg.FileName;
            btnOpen.Enabled = true;
            lblResult.Text = $"Video salvato: {Path.GetFileName(dlg.FileName)} ({frames} frame, {fps} fps)";
        }
        catch (OperationCanceledException)
        {
            lblResult.Text = "Annullato.";
        }
        catch (Exception ex)
        {
            lblResult.Text = "Errore: " + ex.Message;
        }
        finally
        {
            if (tmpDir.Length > 0)
            {
                try { Directory.Delete(tmpDir, recursive: true); } catch { }
            }
            _cts?.Dispose();
            _cts = null;
            _rendering = false;
            btnStart.Text = "Avvia";
            btnClose.Enabled = true;
            progressBar.Value = 0;
        }
    }

    private void RenderFrames(string tmpDir, int frames,
        double startCx, double startCy, double startScale, int effAa,
        IProgress<int> progress, CancellationToken ct)
    {
        for (int i = 0; i < frames; i++)
        {
            ct.ThrowIfCancellationRequested();
            FrameAt(i, frames, startCx, startCy, startScale,
                out double cx, out double cy, out double scale, out int maxIter);
            using var bmp = RenderFrame(cx, cy, scale, maxIter, effAa, ct);
            if (bmp == null) throw new InvalidOperationException("Render non riuscito (DirectX non pronto?).");
            bmp.Save(Path.Combine(tmpDir, $"f{i:0000}.png"),
                System.Drawing.Imaging.ImageFormat.Png);
            progress.Report(i);
        }
    }

    /// <summary>Parametri del frame i-esimo dello zoom (stessa interpolazione del video:
    /// scala geometrica ease-out, centro che segue lo zoom, iterazioni auto).</summary>
    private void FrameAt(int i, int frames, double startCx, double startCy, double startScale,
        out double cx, out double cy, out double scale, out int maxIter)
    {
        double endScale = MandelbrotForm.StartScale;
        double span = endScale - startScale;
        double t = frames > 1 ? i / (double)(frames - 1) : 1.0;
        double te = 1.0 - Math.Pow(1.0 - t, 3.0);
        scale = startScale * Math.Pow(endScale / startScale, te);
        double u = span > 0 ? (scale - startScale) / span : 1.0;
        cx = startCx + (MandelbrotForm.StartCenterX - startCx) * u;
        cy = startCy + (MandelbrotForm.StartCenterY - startCy) * u;
        maxIter = Mandelbrot.AutoIterForScale(scale);
    }

    private Bitmap? RenderFrame(double cx, double cy, double scale, int maxIter, int effAa, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_useCuda)
        {
            var bmp = new Bitmap(_viewW, _viewH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                GpuMandelbrot.Render(bmp, cx, cy, scale, maxIter, _palette, effAa, _useDouble, ct, _jcx, _jcy, _julia);
                return bmp;
            }
            catch
            {
                bmp.Dispose();
                throw;
            }
        }
        if (_useDirectX)
            return DxMandelbrot.RenderPreviewToBitmap(cx, cy, scale, _viewW, _viewH, maxIter, effAa, _palette, _jcx, _jcy, _julia);
        var cpu = new Bitmap(_viewW, _viewH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            Mandelbrot.Render(cpu, cx, cy, scale, maxIter, _palette, effAa, ct, _jcx, _jcy, _julia);
            return cpu;
        }
        catch
        {
            cpu.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Codifica i PNG con ffmpeg (su thread worker: mai sul thread UI). Lo stderr
    /// è drenato in asincrono DURANTE l'attesa: leggerlo dopo il WaitForExit
    /// deadlocka appena il pipe si riempie (ffmpeg logga ogni frame). Su
    /// cancellazione il processo viene ucciso.
    /// </summary>
    private static void RunFfmpeg(string ffmpeg, string tmpDir, int frames, int fps,
        string output, CancellationToken ct)
    {
        // Pre-flight: senza frame l'encode fallisce con "no packets".
        if (Directory.GetFiles(tmpDir, "f*.png").Length == 0)
            throw new InvalidOperationException("Nessun frame renderizzato da codificare.");
        using var proc = new System.Diagnostics.Process();
        proc.StartInfo.FileName = ffmpeg;
        // pad a dimensioni pari: la vista ha spesso lati dispari e yuv420p/libx264
        // li rifiuta ("Could not open encoder" + "no packets", exit 0xDFABA7BB).
        proc.StartInfo.Arguments = $"-y -framerate {fps} -i \"{Path.Combine(tmpDir, "f%04d.png")}\" " +
            $"-frames:v {frames} -vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\" " +
            $"-c:v libx264 -pix_fmt yuv420p -crf 18 \"{output}\"";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.CreateNoWindow = true;
        proc.StartInfo.RedirectStandardError = true;
        var stderr = new System.Text.StringBuilder();
        proc.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        proc.Start();
        proc.BeginErrorReadLine();
        try
        {
            while (!proc.WaitForExit(500))
                ct.ThrowIfCancellationRequested();
            proc.WaitForExit(); // ricongiunge i reader asincroni
        }
        catch
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        if (proc.ExitCode != 0)
            throw new InvalidOperationException(
                "ffmpeg fallito (exit " + proc.ExitCode + "): " + LastLines(stderr.ToString(), 10));
    }

    private static string LastLines(string text, int n)
    {
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" | ", lines.Skip(Math.Max(0, lines.Length - n)));
    }

    /// <summary>Apre il risultato (MP4 col player predefinito, cartella PNG in Explorer).</summary>
    private void BtnOpen_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_resultPath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_resultPath)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            lblResult.Text = "Apertura fallita: " + ex.Message;
        }
    }

    private void BtnClose_Click(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnFormClosing(e);
    }
}
