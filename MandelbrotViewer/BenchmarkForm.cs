namespace MandelbrotViewer;

/// <summary>
/// Benchmark standard: zona fissa ad alte iterazioni per 8 secondi, misura le
/// iterazioni al secondo del motore selezionato (con fallback CPU).
/// </summary>
public partial class BenchmarkForm : Form
{
    // Parametri standard del test (fissi, così i risultati sono confrontabili).
    private const int BW = 960;
    private const int BH = 540;
    private const int BMaxIter = 5000;
    private const int BAA = 8; // il test gira in AA 8x (64x pixel per frame)
    private const double BCx = -0.743643887037151; // valle dei cavallucci marini
    private const double BCy = 0.131825904205330;
    private const double BScale = 0.0005;
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(8);

    private readonly RenderEngine _engine;
    private readonly bool _useCuda;
    private readonly bool _useDirectX;
    private readonly bool _useDouble;
    private CancellationTokenSource? _cts;
    private bool _running;
    private double _measuredMpixel;

    public BenchmarkForm(RenderEngine engine, bool useDouble)
    {
        InitializeComponent();

        _useCuda = engine == RenderEngine.Cuda && GpuMandelbrot.IsReady;
        _useDirectX = engine == RenderEngine.DirectX && DxMandelbrot.IsReady;
        _useDouble = useDouble;
        _engine = _useDirectX ? RenderEngine.DirectX : _useCuda ? RenderEngine.Cuda : RenderEngine.Cpu;
        string note = engine switch
        {
            RenderEngine.Cuda when !_useCuda => " (CUDA non pronta, uso CPU)",
            RenderEngine.DirectX when !_useDirectX => " (DirectX non pronto, uso CPU)",
            _ => "",
        };
        string precision = _useCuda ? $"CUDA {(_useDouble ? "64-bit" : "32-bit")}" : _useDirectX ? "DirectX float" : "CPU";
        lblInfo.Text = $"Motore: {RenderEngineInfo.DisplayName(_engine)}{note}\n" +
            $"Zona {BW}x{BH} {BAA}x AA = {BW * BAA}x{BH * BAA} campioni elementari senza media, " +
            $"{BMaxIter} iterazioni max, scala {BScale}, precisione {precision} — " +
            $"durata minima {Budget.TotalSeconds:F0} secondi (DirectX senza v-sync).";
        lblResult.Text = "—";
        chartPanel.Invalidate();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        BeginInvoke((MethodInvoker)(() =>
        {
            if (!IsDisposed) BtnStart_Click(this, EventArgs.Empty);
        }));
    }

    private static string FormatPixels(double perSec) => perSec switch
    {
        >= 1e9 => $"{perSec / 1e9:F2} Gpixel/s",
        >= 1e6 => $"{perSec / 1e6:F2} Mpixel/s",
        >= 1e3 => $"{perSec / 1e3:F1} kpixel/s",
        _ => $"{perSec:F0} pixel/s",
    };

    private double PixelsPerSecond(int frames, double seconds) =>
        seconds > 0 ? frames * (long)BW * BH * BAA * BAA / seconds : 0;

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_running) { _cts?.Cancel(); return; }

        _running = true;
        _cts = new CancellationTokenSource();
        btnStart.Text = "Annulla";
        btnClose.Enabled = false;
        lblResult.Text = "…";
        lblDetail.Text = "";
        lblLive.Text = "0%  |  —";
        lblLive.Refresh();

        // Primo frame della zona di benchmark reso visibile anche per i motori
        // che non disegnano su una bitmap nella finestra Benchmark: DirectX rende
        // offscreen (colored) e lo mostra in previewBox, come CUDA/CPU.
        if (_useDirectX)
        {
            try
            {
                var preview = await Task.Run(
                    () => DxMandelbrot.RenderPreviewToBitmap(BCx, BCy, BScale, BW, BH, BMaxIter, 1, Palette.Fuoco),
                    _cts.Token);
                if (preview != null && !IsDisposed)
                {
                    var old = previewBox.Image;
                    previewBox.Image = preview;
                    previewBox.Visible = true;
                    old?.Dispose();
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }
        else
        {
            try
            {
                var preview = await Task.Run(() => RenderBenchmarkPreview(_cts.Token), _cts.Token);
                if (preview != null && !IsDisposed)
                {
                    var old = previewBox.Image;
                    previewBox.Image = preview;
                    previewBox.Visible = true;
                    old?.Dispose();
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }

        IProgress<BenchmarkProgress> progress = _useDirectX
            ? new Progress<BenchmarkProgress>(UpdateProgress)
            : new Progress<BenchmarkProgress>(UpdateProgress);

        try
        {
            await Task.Delay(150, _cts.Token);
            double seconds;
            int frames;
            if (_useDirectX)
            {
                (seconds, frames) = await Task.Run(
                    () => BenchmarkDirectX(progress, _cts.Token),
                    _cts.Token);
            }
            else
            {
                ( _, seconds, frames) = await Task.Run(() =>
                    _useCuda
                        ? GpuMandelbrot.BenchmarkGpu(BCx, BCy, BScale, BW, BH, BMaxIter, BAA, _useDouble, Budget, progress, _cts.Token)
                        : Mandelbrot.BenchmarkCpu(BCx, BCy, BScale, BW, BH, BMaxIter, BAA, Budget, progress, _cts.Token),
                    _cts.Token);
            }

            _measuredMpixel = PixelsPerSecond(frames, seconds) / 1e6;
            lblResult.Text = FormatPixels(_measuredMpixel * 1e6);
            lblDetail.Text = _useDirectX
                ? $"{frames} frame ({frames / seconds:F1} frame/s) {BW}x{BH} AA{BAA}x in {seconds:F1} s"
                : $"{frames} frame {BW}x{BH} AA{BAA}x in {seconds:F1} s";
            lblLive.Text = "";
            chartPanel.Invalidate();
        }
        catch (OperationCanceledException)
        {
            lblResult.Text = "Annullato";
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _running = false;
            btnStart.Text = "Avvia";
            btnClose.Enabled = true;
        }
    }

    private void UpdateProgress(BenchmarkProgress progress)
    {
        double fraction = Math.Clamp(progress.ElapsedSeconds / Budget.TotalSeconds, 0.0, 1.0);
        string liveRate = progress.ElapsedSeconds > 0.2
            ? FormatPixels(PixelsPerSecond(progress.Frames, progress.ElapsedSeconds))
            : "—";
        lblLive.Text = $"{fraction * 100:0}%  |  {liveRate}";
        lblLive.Refresh();
    }

    private void ChartPanel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(chartPanel.BackColor);
        const double referenceCuda = 5940.0;
        const double referenceDirectX = 1750.0;
        const double referenceCpu = 30.0;
        double maximum = Math.Max(referenceCuda, Math.Max(referenceDirectX, Math.Max(referenceCpu, _measuredMpixel))) * 1.15;
        float left = 108;
        float right = 56;
        float top = 22;
        float bottom = 24;
        float plotWidth = chartPanel.ClientSize.Width - left - right;
        float plotHeight = chartPanel.ClientSize.Height - top - bottom;
        if (plotWidth <= 0 || plotHeight <= 0) return;

        using var titleFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 7.5f);
        using var gridPen = new Pen(Color.Gainsboro);
        using var axisPen = new Pen(Color.Gray);
        using var textBrush = new SolidBrush(Color.DimGray);
        using var titleBrush = new SolidBrush(Color.FromArgb(45, 45, 48));
        using var actualBrush = new SolidBrush(Color.FromArgb(36, 113, 163));
        using var cudaBrush = new SolidBrush(Color.FromArgb(226, 126, 34));
        using var cpuBrush = new SolidBrush(Color.FromArgb(112, 128, 144));
        using var centered = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var rightAligned = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

        e.Graphics.DrawString("Confronto prestazioni (MPixel/s)", titleFont, titleBrush, left, 2);
        for (int step = 0; step <= 2; step++)
        {
            float x = left + plotWidth * step / 2f;
            e.Graphics.DrawLine(gridPen, x, top, x, top + plotHeight);
            string value = $"{maximum * step / 2:0}";
            e.Graphics.DrawString(value, labelFont, textBrush, new RectangleF(x - 24, top + plotHeight + 1, 48, 16), centered);
        }
        e.Graphics.DrawLine(axisPen, left, top, left, top + plotHeight);

        (string Label, double Value, Brush Brush)[] bars =
        [
            ("Risultato", _measuredMpixel, actualBrush),
            ("CUDA 5070 Ti", referenceCuda, cudaBrush),
            ("DirectX 5070 Ti", referenceDirectX, cpuBrush),
            ("CPU", referenceCpu, cpuBrush),
        ];
        float rowHeight = plotHeight / bars.Length;
        float barHeight = Math.Min(22f, rowHeight * 0.62f);
        for (int i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];
            float width = (float)(Math.Max(0, bar.Value) / maximum * plotWidth);
            float x = left;
            float y = top + rowHeight * i + (rowHeight - barHeight) / 2f;
            e.Graphics.DrawString(bar.Label, labelFont, textBrush, new RectangleF(0, y, left - 10, barHeight), rightAligned);
            e.Graphics.FillRectangle(bar.Brush, x, y, width, barHeight);
            string value = bar.Value > 0 ? $"{bar.Value:0.#}" : "—";
            float valueX = Math.Min(x + width + 4, chartPanel.ClientSize.Width - right + 4);
            e.Graphics.DrawString(value, labelFont, titleBrush, valueX, y + (barHeight - labelFont.Height) / 2f);
        }
    }

    private void BtnClose_Click(object? sender, EventArgs e) => Close();

    /// <summary>
    /// Primo frame della zona di benchmark (960x540, AA1x) reso visibile per i
    /// motori che non disegnano su una swapchain (CUDA e CPU): mostra la zona
    /// testata, come DirectX fa già con il primo frame sulla swapchain.
    /// </summary>
    private Bitmap? RenderBenchmarkPreview(CancellationToken ct)
    {
        var bmp = new Bitmap(BW, BH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            if (_useCuda)
                GpuMandelbrot.Render(bmp, BCx, BCy, BScale, BMaxIter, Palette.Fuoco, 1, _useDouble, ct);
            else
                Mandelbrot.Render(bmp, BCx, BCy, BScale, BMaxIter, Palette.Fuoco, 1, ct);
            return bmp;
        }
        catch
        {
            bmp.Dispose();
            return null;
        }
    }

    /// <summary>
    /// Benchmark DirectX standardizzato: rende la griglia dei campioni elementari
    /// (960x540 AA8x = 7680x4320 pixel) con lo shader solo-iterazioni, senza media
    /// dei campioni e senza v-sync (Present(0)). Il lavoro per frame è quindi
    /// identico a quello dei benchmark CUDA e CPU.
    /// </summary>
    private static async Task<(double Seconds, int Frames)> BenchmarkDirectX(IProgress<BenchmarkProgress> progress, CancellationToken ct)
    {
        int gridW = BW * BAA;
        int gridH = BH * BAA;
        DxMandelbrot.BeginBenchmark(gridW, gridH);
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int frames = 0;
            TimeSpan lastReport = TimeSpan.Zero;
            while (sw.Elapsed < Budget)
            {
                ct.ThrowIfCancellationRequested();
                DxMandelbrot.RenderBenchmark(BCx, BCy, BScale, gridW, gridH, BMaxIter);
                frames++;
                if (sw.Elapsed - lastReport >= BenchmarkProgress.ReportInterval)
                {
                    progress.Report(new BenchmarkProgress(sw.Elapsed.TotalSeconds, 0, frames));
                    lastReport = sw.Elapsed;
                }
                await Task.Yield();
            }
            progress.Report(new BenchmarkProgress(sw.Elapsed.TotalSeconds, 0, frames));
            return (sw.Elapsed.TotalSeconds, frames);
        }
        finally
        {
            DxMandelbrot.EndBenchmark();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        previewBox.Image?.Dispose();
        _cts?.Cancel();
        base.OnFormClosing(e);
    }
}
