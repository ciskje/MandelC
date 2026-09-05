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
    private CancellationTokenSource? _cts;
    private bool _running;

    public BenchmarkForm(RenderEngine engine)
    {
        InitializeComponent();

        _useCuda = engine == RenderEngine.Cuda && GpuMandelbrot.IsReady;
        _engine = _useCuda ? RenderEngine.Cuda : RenderEngine.Cpu;
        string note = engine switch
        {
            RenderEngine.Cuda when !_useCuda => " (CUDA non pronta, uso CPU)",
            RenderEngine.DirectX => " (DirectX non misurabile, uso CPU)",
            _ => "",
        };
        lblInfo.Text = $"Motore: {RenderEngineInfo.DisplayName(_engine)}{note}\n" +
            $"Zona standard {BW}x{BH} AA{BAA}x, {BMaxIter} iterazioni max, scala {BScale} — durata minima {Budget.TotalSeconds:F0} secondi.";
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
            var (_, seconds, frames) = await Task.Run(() =>
                _useCuda
                    ? GpuMandelbrot.BenchmarkGpu(BCx, BCy, BScale, BW, BH, BMaxIter, BAA, Budget, progress, _cts.Token)
                    : Mandelbrot.BenchmarkCpu(BCx, BCy, BScale, BW, BH, BMaxIter, BAA, Budget, progress, _cts.Token),
                _cts.Token);

            progressBar.Value = progressBar.Maximum;
            lblResult.Text = FormatPixels(PixelsPerSecond(frames, seconds));
            lblDetail.Text = $"{frames} frame {BW}x{BH} AA{BAA}x in {seconds:F1} s";
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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnFormClosing(e);
    }
}
