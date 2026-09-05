using System.Drawing.Imaging;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

namespace MandelbrotViewer;

/// <summary>Parametri della vista passati ai kernel (struct blittabile, deve essere public per ILGPU).</summary>
public readonly struct GpuViewParams
{
    public readonly double CenterX;
    public readonly double CenterY;
    public readonly double PixelSize;
    public readonly double TopY;
    public readonly int W;
    public readonly int H;
    public readonly int MaxIter;

    public GpuViewParams(double centerX, double centerY, double pixelSize, double topY, int w, int h, int maxIter)
    {
        CenterX = centerX;
        CenterY = centerY;
        PixelSize = pixelSize;
        TopY = topY;
        W = w;
        H = h;
        MaxIter = maxIter;
    }
}

/// <summary>Frame calcolato su GPU: iterazioni e |z|² finali per pixel.</summary>
internal sealed class GpuFrame
{
    public required int[] Iters;
    public required double[] Mod2;
    public required bool UsedDouble;
}

/// <summary>
/// Backend CUDA via ILGPU: un thread GPU per pixel calcola solo la fuga
/// (`z = z² + c`, solo aritmetica, niente Math sul device); la mappatura
/// palette resta su CPU e riusa <see cref="Mandelbrot.ColorFromEscape"/>.
/// Soglia float/double: sotto scala 1e-3 il float non basta più.
/// </summary>
internal static class GpuMandelbrot
{
    private static Context? _context;
    private static Accelerator? _accelerator;
    private static Action<Index1D, ArrayView<int>, ArrayView<double>, GpuViewParams>? _floatKernel;
    private static Action<Index1D, ArrayView<int>, ArrayView<double>, GpuViewParams>? _doubleKernel;
    // Kernel benchmark: solo conteggio iterazioni, niente buffer |z|² (un terzo del traffico).
    private static Action<Index1D, ArrayView<int>, GpuViewParams>? _floatBenchKernel;
    private static Action<Index1D, ArrayView<int>, GpuViewParams>? _doubleBenchKernel;

    public static bool IsReady => _accelerator != null;
    public static string DeviceName { get; private set; } = "";
    public static string DeviceShortName => DeviceName.Replace("NVIDIA GeForce ", "");
    /// <summary>Motivo dell'ultima inizializzazione fallita (diagnostica).</summary>
    public static string LastError { get; private set; } = "";

    /// <summary>True se la scala richiede il double (il float non ha cifre a sufficienza).</summary>
    public static bool WantsDouble(double scale) => scale < 1e-3;

    /// <summary>Device CUDA disponibili (nomi ILGPU); vuoto se nessun CUDA.</summary>
    public static IReadOnlyList<string> DeviceNames()
    {
        try
        {
            _context ??= Context.CreateDefault();
            return _context.Devices
                .Where(d => d.AcceleratorType == AcceleratorType.Cuda)
                .Select(d => d.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Inizializza contesto, device CUDA e kernel. Con <paramref name="deviceName"/>
    /// usa quella scheda; senza, prova i device CUDA dal più capiente.
    /// Ritorna false se nessuno funziona (si usa la CPU).
    /// </summary>
    public static bool TryInitialize(string? deviceName = null)
    {
        if (IsReady && (deviceName == null || DeviceName == deviceName)) return true;
        LastError = "";
        ResetAccelerator();
        try
        {
            _context ??= Context.CreateDefault();
            var cudaDevices = _context.Devices
                .Where(d => d.AcceleratorType == AcceleratorType.Cuda)
                .ToList();
            if (cudaDevices.Count == 0)
            {
                LastError = "Nessun device CUDA enumerato.";
                return false;
            }

            var candidates = deviceName == null
                ? cudaDevices.OrderByDescending(d => d.MemorySize).ToList()
                : cudaDevices.Where(d => d.Name == deviceName).ToList();
            if (candidates.Count == 0)
            {
                LastError = $"Device CUDA non trovato: {deviceName}";
                return false;
            }

            foreach (var device in candidates)
            {
                try
                {
                    _accelerator = device.CreateAccelerator(_context);
                    DeviceName = device.Name;
                    _floatKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, ArrayView<double>, GpuViewParams>(FloatKernel);
                    _doubleKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, ArrayView<double>, GpuViewParams>(DoubleKernel);
                    _floatBenchKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, GpuViewParams>(FloatBenchKernel);
                    _doubleBenchKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, GpuViewParams>(DoubleBenchKernel);
                    return true;
                }
                catch (Exception ex)
                {
                    LastError = $"{device.Name}: {ex.GetType().Name}: {ex.Message}";
                    ResetAccelerator();
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            LastError = ex.GetType().Name + ": " + ex.Message;
            ResetAccelerator();
            return false;
        }
    }

    /// <summary>Scarica accelerator+kernel (il contesto CUDA resta riusabile).</summary>
    private static void ResetAccelerator()
    {
        _floatKernel = null;
        _doubleKernel = null;
        _floatBenchKernel = null;
        _doubleBenchKernel = null;
        _accelerator?.Dispose();
        _accelerator = null;
        DeviceName = "";
    }

    /// <summary>Calcola il frame su GPU (lancio kernel + ricopia in RAM).</summary>
    /// <param name="supersample">Antialias: risoluzione k volte maggiore (1 = nessuno).</param>
    public static GpuFrame RenderFrame(double centerX, double centerY, double scale, int w, int h, int maxIter, int supersample, CancellationToken ct)
    {
        if (!IsReady) throw new InvalidOperationException("GPU non inizializzata.");
        bool useDouble = WantsDouble(scale);
        int k = Math.Max(1, supersample);
        int bigW = w * k;
        int bigH = h * k;
        double pixelSize = scale / bigW;
        double topY = centerY - (bigH * 0.5) * pixelSize;
        var pars = new GpuViewParams(centerX, centerY, pixelSize, topY, bigW, bigH, maxIter);

        int count = bigW * bigH;
        using var itersBuf = _accelerator!.Allocate1D<int>(count);
        using var modBuf = _accelerator.Allocate1D<double>(count);

        var kernel = useDouble ? _doubleKernel! : _floatKernel!;
        kernel(count, itersBuf.View, modBuf.View, pars);
        _accelerator.Synchronize();
        ct.ThrowIfCancellationRequested();

        var iters = new int[count];
        var mod = new double[count];
        itersBuf.CopyToCPU(iters);
        modBuf.CopyToCPU(mod);
        return new GpuFrame { Iters = iters, Mod2 = mod, UsedDouble = useDouble };
    }

    /// <summary>Colora il frame GPU nel bitmap (stessa palette del percorso CPU).</summary>
    public static void RenderToBitmap(Bitmap bmp, GpuFrame frame, int maxIter, Palette palette, int supersample, CancellationToken ct)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        int k = Math.Max(1, supersample);
        int bigW = w * k;

        var rect = new Rectangle(0, 0, w, h);
        BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = data.Stride / 4;
            int[] big = new int[bigW * (h * k)];

            Parallel.For(0, h * k, new ParallelOptions { CancellationToken = ct }, by =>
            {
                for (int bx = 0; bx < bigW; bx++)
                {
                    int i = by * bigW + bx;
                    big[i] = Mandelbrot.ColorFromEscape(frame.Iters[i], frame.Mod2[i], maxIter, palette);
                }
            });

            int[] pixels = new int[stride * h];
            Parallel.For(0, h, new ParallelOptions { CancellationToken = ct }, y =>
            {
                for (int x = 0; x < w; x++)
                    pixels[y * stride + x] = Mandelbrot.AverageBlock(big, bigW, x * k, y * k, k);
            });

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    /// <summary>
    /// Benchmark GPU: ripete frame completi (kernel + ricopia) per il budget dato
    /// e somma le iterazioni. Ritorna (iterazioni, secondi, frame).
    /// Il progresso alla UI è limitato (ogni 3 s) per non falsare la misura.
    /// </summary>
    public static (long TotalIters, double Seconds, int Frames) BenchmarkGpu(double centerX, double centerY, double scale, int w, int h, int maxIter, int supersample, TimeSpan budget, IProgress<BenchmarkProgress>? progress, CancellationToken ct)
    {
        if (!IsReady) throw new InvalidOperationException("GPU non inizializzata.");

        // Buffer allocati una volta sola e riusati per tutti i frame (niente alloc/copy extra).
        int k = Math.Max(1, supersample);
        int bigW = w * k;
        int bigH = h * k;
        double pixelSize = scale / bigW;
        double topY = centerY - (bigH * 0.5) * pixelSize;
        var pars = new GpuViewParams(centerX, centerY, pixelSize, topY, bigW, bigH, maxIter);
        int count = bigW * bigH;
        bool useDouble = WantsDouble(scale);
        var kernel = useDouble ? _doubleBenchKernel! : _floatBenchKernel!;
        using var itersBuf = _accelerator!.Allocate1D<int>(count);
        var iters = new int[count];

        var sw = System.Diagnostics.Stopwatch.StartNew();
        long total = 0;
        int frames = 0;
        bool first = true;
        TimeSpan lastReport = TimeSpan.Zero;

        while (sw.Elapsed < budget)
        {
            ct.ThrowIfCancellationRequested();
            kernel(count, itersBuf.View, pars);
            _accelerator.Synchronize();
            ct.ThrowIfCancellationRequested();
            itersBuf.CopyToCPU(iters);
            long sum = 0;
            foreach (int v in iters) sum += v;
            total += sum;
            frames++;
            if (first || sw.Elapsed - lastReport >= BenchmarkProgress.ReportInterval)
            {
                progress?.Report(new BenchmarkProgress(sw.Elapsed.TotalSeconds, total, frames));
                lastReport = sw.Elapsed;
                first = false;
            }
        }

        progress?.Report(new BenchmarkProgress(sw.Elapsed.TotalSeconds, total, frames));
        return (total, sw.Elapsed.TotalSeconds, frames);
    }

    public static void Dispose()
    {
        ResetAccelerator();
        _context?.Dispose();
        _context = null;
    }

    // ---------- Kernel benchmark: solo iterazioni (niente |z|², un terzo del traffico) ----------

    private static void FloatBenchKernel(Index1D index, ArrayView<int> iters, GpuViewParams p)
    {
        int i = index.X;
        int x = i % p.W;
        int y = i / p.W;
        float pixel = (float)p.PixelSize;
        float cx = (float)p.CenterX + (x - p.W * 0.5f) * pixel;
        float cy = (float)p.TopY + y * pixel;

        float zx = 0, zy = 0, zx2 = 0, zy2 = 0;
        int iter = 0;
        while (iter < p.MaxIter && zx2 + zy2 <= 4f)
        {
            zy = 2 * zx * zy + cy;
            zx = zx2 - zy2 + cx;
            zx2 = zx * zx;
            zy2 = zy * zy;
            ++iter;
        }
        iters[index] = iter;
    }

    private static void DoubleBenchKernel(Index1D index, ArrayView<int> iters, GpuViewParams p)
    {
        int i = index.X;
        int x = i % p.W;
        int y = i / p.W;
        double cx = p.CenterX + (x - p.W * 0.5) * p.PixelSize;
        double cy = p.TopY + y * p.PixelSize;

        double zx = 0, zy = 0, zx2 = 0, zy2 = 0;
        int iter = 0;
        while (iter < p.MaxIter && zx2 + zy2 <= 4.0)
        {
            zy = 2 * zx * zy + cy;
            zx = zx2 - zy2 + cx;
            zx2 = zx * zx;
            zy2 = zy * zy;
            ++iter;
        }
        iters[index] = iter;
    }

    // ---------- Kernel (solo aritmetica: niente Math sul device) ----------

    private static void FloatKernel(Index1D index, ArrayView<int> iters, ArrayView<double> mod2, GpuViewParams p)
    {
        int i = index.X;
        int x = i % p.W;
        int y = i / p.W;
        float pixel = (float)p.PixelSize;
        float cx = (float)p.CenterX + (x - p.W * 0.5f) * pixel;
        float cy = (float)p.TopY + y * pixel;

        float zx = 0, zy = 0, zx2 = 0, zy2 = 0;
        int iter = 0;
        while (iter < p.MaxIter && zx2 + zy2 <= 4f)
        {
            zy = 2 * zx * zy + cy;
            zx = zx2 - zy2 + cx;
            zx2 = zx * zx;
            zy2 = zy * zy;
            ++iter;
        }
        iters[index] = iter;
        mod2[index] = zx2 + zy2;
    }

    private static void DoubleKernel(Index1D index, ArrayView<int> iters, ArrayView<double> mod2, GpuViewParams p)
    {
        int i = index.X;
        int x = i % p.W;
        int y = i / p.W;
        double cx = p.CenterX + (x - p.W * 0.5) * p.PixelSize;
        double cy = p.TopY + y * p.PixelSize;

        double zx = 0, zy = 0, zx2 = 0, zy2 = 0;
        int iter = 0;
        while (iter < p.MaxIter && zx2 + zy2 <= 4.0)
        {
            zy = 2 * zx * zy + cy;
            zx = zx2 - zy2 + cx;
            zx2 = zx * zx;
            zy2 = zy * zy;
            ++iter;
        }
        iters[index] = iter;
        mod2[index] = zx2 + zy2;
    }
}
