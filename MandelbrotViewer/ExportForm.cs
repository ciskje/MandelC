using System.Drawing.Imaging;

namespace MandelbrotViewer;

/// <summary>
/// Export PNG ad alta risoluzione della vista corrente: rende offscreen con il
/// motore attivo (CPU/CUDA/DirectX) alla larghezza scelta e salva il PNG.
/// L'AA impostato viene ridotto in automatico se i campioni totali superano il
/// tetto (per non esaurire la VRAM); annullabile durante il render.
/// </summary>
public partial class ExportForm : Form
{
    /// <summary>Tetto campioni totali (larghezza×AA · altezza×AA): oltre, l'AA scende.</summary>
    private const long MaxSamples = 134_217_728; // 128 MPixel (4x la griglia benchmark)

    private readonly double _cx, _cy, _scale;
    private readonly int _maxIter;
    private readonly Palette _palette;
    private readonly int _aa;
    private readonly bool _useCuda;
    private readonly bool _useDirectX;
    private readonly bool _useDouble;
    private readonly bool _julia;
    private readonly double _jcx, _jcy;
    private readonly double _aspect;
    private readonly int _viewW0, _viewH0;
    private CancellationTokenSource? _cts;
    private bool _rendering;

    public ExportForm(double cx, double cy, double scale, int maxIter, Palette palette, int aa,
        RenderEngine engine, bool useCuda, bool useDouble, Size viewSize,
        bool julia = false, double juliaCx = 0, double juliaCy = 0, int presetDefault = -1)
    {
        InitializeComponent();
        if (presetDefault >= 0 && presetDefault < cmbPreset.Items.Count)
            cmbPreset.SelectedIndex = presetDefault; // screenshot: parte da Vista, tutto libero
        Program.ApplyIcon(this);

        _cx = cx;
        _cy = cy;
        _scale = scale;
        _maxIter = maxIter;
        _palette = palette;
        _aa = Math.Max(1, aa);
        _useCuda = useCuda;
        _useDirectX = engine == RenderEngine.DirectX && DxMandelbrot.IsReady;
        _useDouble = useDouble;
        _julia = julia;
        _jcx = juliaCx;
        _jcy = juliaCy;
        _aspect = Math.Max(1, viewSize.Height) / (double)Math.Max(1, viewSize.Width);
        _viewW0 = Math.Max(320, viewSize.Width);
        _viewH0 = Math.Max(240, viewSize.Height);
        txtW.Text = _viewW0.ToString();
        txtH.Text = _viewH0.ToString();
        UpdateCustomEnabled();

        string engineLabel = _useCuda ? $"CUDA {(_useDouble ? "64-bit" : "32-bit")}"
            : _useDirectX ? "DirectX" : "CPU";
        string modeLabel = _julia ? $"Julia c={_jcx:+0.000000;-0.000000} {_jcy:+0.000000;-0.000000}i" : "Mandelbrot";
        lblInfo.Text = $"Motore: {engineLabel} — {modeLabel}, {_palette}, {maxIter} iterazioni. " +
            "L'AA viene ridotto in automatico oltre 128 MPixel di campioni.";
        UpdateInfo();
    }

    /// <summary>Risoluzione e AA richiesti: preset, o caselle custom validate
    /// (interi 320…16384); l'AA effettivo scende a potenze di 2 oltre il tetto.</summary>
    private bool ResolveSettings(out int w, out int h, out int reqAa, out int effAa)
    {
        w = h = reqAa = effAa = 0;
        switch (cmbPreset.SelectedIndex)
        {
            case 1: w = 1920; h = 1080; break; // Full HD
            case 2: w = 2560; h = 1440; break; // 2K
            case 3: w = 3840; h = 2160; break; // 4K
            case 4: w = 7680; h = 4320; break; // 8K
            case 5: w = 7680; h = 2160; break; // doppio 4K 16:9+16:9
            case 6: // personalizzata: validazione stretta
                if (!int.TryParse(txtW.Text.Trim(), out w)
                    || !int.TryParse(txtH.Text.Trim(), out h)
                    || w < 320 || w > 16384 || h < 320 || h > 16384)
                    return false;
                break;
            default: w = _viewW0; h = _viewH0; break; // vista corrente
        }
        reqAa = cmbAAExp.SelectedIndex <= 0 ? _aa : 1 << (cmbAAExp.SelectedIndex - 1);
        effAa = reqAa;
        while ((long)w * effAa * h * effAa > MaxSamples && effAa > 1)
            effAa /= 2;
        return true;
    }

    private void UpdateInfo()
    {
        if (!ResolveSettings(out int w, out int h, out int reqAa, out int effAa))
        {
            lblResult.Text = "Dimensioni personalizzate: interi 320…16384 px.";
            return;
        }
        double mpixel = w * (double)h / 1e6;
        string aaNote = effAa < reqAa ? $" (AA ridotto da {reqAa}x: oltre il tetto)" : "";
        lblResult.Text = $"{w}×{h} ({mpixel:0.#} MPixel), AA{effAa}x{aaNote} — " +
            $"{w * (long)effAa * h * effAa / 1e6:0.#} MPixel di campioni.";
    }

    private void CmbPreset_Changed(object? sender, EventArgs e)
    {
        UpdateCustomEnabled();
        UpdateInfo();
    }

    private void UpdateCustomEnabled()
    {
        bool custom = cmbPreset.SelectedIndex == 6;
        txtW.Enabled = custom;
        txtH.Enabled = custom;
    }

    private void CmbWidth_Changed(object? sender, EventArgs e) => UpdateInfo();

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_rendering) { _cts?.Cancel(); return; }
        if (!ResolveSettings(out int w, out int h, out _, out int effAa))
        {
            lblResult.Text = "Dimensioni personalizzate: interi 320…16384 px.";
            return;
        }

        _rendering = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        btnStart.Text = "Annulla";
        btnClose.Enabled = false;
        progressBar.Visible = true;
        lblResult.Text = $"Render {w}×{h} AA{effAa}x in corso…";

        try
        {
            Bitmap? bmp = await Task.Run(() => RenderExport(w, h, effAa, token), token);
            try
            {
                if (IsDisposed) return;
                token.ThrowIfCancellationRequested();
                if (bmp == null)
                {
                    lblResult.Text = "Render non riuscito (DirectX non pronto?).";
                    return;
                }
                using var dlg = new SaveFileDialog
                {
                    Filter = "PNG (*.png)|*.png",
                    FileName = "mandelbrot.png",
                    DefaultExt = "png",
                };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                bmp.Save(dlg.FileName, ImageFormat.Png);
                lblResult.Text = $"Salvato {w}×{h} AA{effAa}x in {Path.GetFileName(dlg.FileName)}";
            }
            finally
            {
                bmp?.Dispose();
            }
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
            _cts?.Dispose();
            _cts = null;
            _rendering = false;
            btnStart.Text = "Avvia";
            btnClose.Enabled = true;
            progressBar.Visible = false;
        }
    }

    /// <summary>Rende la vista alla risoluzione scelta col motore attivo
    /// (null se DirectX non pronto; la preview DX è sincrona e non cancellabile
    /// — la chiusura scarta il risultato).</summary>
    private Bitmap? RenderExport(int w, int h, int aa, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_useCuda)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            try
            {
                GpuMandelbrot.Render(bmp, _cx, _cy, _scale, _maxIter, _palette, aa, _useDouble, ct, _jcx, _jcy, _julia);
                return bmp;
            }
            catch
            {
                bmp.Dispose();
                throw;
            }
        }
        if (_useDirectX)
            return DxMandelbrot.RenderPreviewToBitmap(_cx, _cy, _scale, w, h, _maxIter, aa, _palette, _jcx, _jcy, _julia);
        var cpu = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        try
        {
            Mandelbrot.Render(cpu, _cx, _cy, _scale, _maxIter, _palette, aa, ct, _jcx, _jcy, _julia);
            return cpu;
        }
        catch
        {
            cpu.Dispose();
            throw;
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
