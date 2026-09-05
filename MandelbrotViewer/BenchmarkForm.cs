namespace MandelbrotViewer;

/// <summary>
/// Benchmark standard: zona fissa ad alte iterazioni per 8 secondi, misura le
/// iterazioni al secondo del motore selezionato (con fallback CPU).
/// </summary>
public partial class BenchmarkForm : Form
{
    // Parametri standard del test (fissi, così i risultati sono confrontabili).
    private const int BW = 800;
    private const int BH = 600;
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
            $"Zona standard {BW}x{BH} AA{BAA}x, {BMaxIter} iterazioni max, scala {BScale}, " +
            $"precisione {precision} — durata minima {Budget.TotalSeconds:F0} secondi.";
        lblResult.Text = "—";
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
        lblLive.Text = "";

        var progress = new Progress<BenchmarkProgress>(p =>
        {
            progressBar.Value = (int)Math.Clamp(p.ElapsedSeconds / Budget.TotalSeconds * 1000, 0, 1000);
            if (p.ElapsedSeconds > 0.2)
                lblLive.Text = FormatPixels(PixelsPerSecond(p.Frames, p.ElapsedSeconds));
        });

        try
        {
            double seconds;
            int frames;
            if (_useDirectX)
            {
                (seconds, frames) = await BenchmarkDirectX(progress, _cts.Token);
            }
            else
            {
                ( _, seconds, frames) = await Task.Run(() =>
                    _useCuda
                        ? GpuMandelbrot.BenchmarkGpu(BCx, BCy, BScale, BW, BH, BMaxIter, BAA, _useDouble, Budget, progress, _cts.Token)
                        : Mandelbrot.BenchmarkCpu(BCx, BCy, BScale, BW, BH, BMaxIter, BAA, Budget, progress, _cts.Token),
                    _cts.Token);
            }

            progressBar.Value = progressBar.Maximum;
            lblResult.Text = FormatPixels(PixelsPerSecond(frames, seconds));
            lblDetail.Text = _useDirectX
                ? $"{frames} frame ({frames / seconds:F1} frame/s) {BW}x{BH} AA{BAA}x in {seconds:F1} s"
                : $"{frames} frame {BW}x{BH} AA{BAA}x in {seconds:F1} s";
            lblLive.Text = "";
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

    private void BtnClose_Click(object? sender, EventArgs e) => Close();

    private static async Task<(double Seconds, int Frames)> BenchmarkDirectX(IProgress<BenchmarkProgress> progress, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int frames = 0;
        TimeSpan lastReport = TimeSpan.Zero;
        while (sw.Elapsed < Budget)
        {
            ct.ThrowIfCancellationRequested();
            DxMandelbrot.Render(BCx, BCy, BScale, BW, BH, BMaxIter, BAA, Palette.Fuoco);
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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnFormClosing(e);
    }
}
