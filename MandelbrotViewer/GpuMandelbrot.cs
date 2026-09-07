using System.Drawing.Imaging;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

namespace MandelbrotViewer;

/// <summary>View parameters passed to the kernels (blittable struct, must be public for ILGPU).</summary>
public readonly struct GpuViewParams
{
    public readonly double CenterX;
    public readonly double CenterY;
    public readonly double PixelSize;
    public readonly double TopY;
    public readonly int W;
    public readonly int H;
    public readonly int MaxIter;
    public readonly int Supersample;
    public readonly int JuliaOn;
    public readonly double Jcx;
    public readonly double Jcy;

    public GpuViewParams(double centerX, double centerY, double pixelSize, double topY, int w, int h, int maxIter, int supersample = 1, int juliaOn = 0, double jcx = 0, double jcy = 0)
    {
        CenterX = centerX;
        CenterY = centerY;
        PixelSize = pixelSize;
        TopY = topY;
        W = w;
        H = h;
        MaxIter = maxIter;
        Supersample = supersample;
        JuliaOn = juliaOn;
        Jcx = jcx;
        Jcy = jcy;
    }
}

public readonly struct GpuPaletteParams
{
    public readonly float S0R, S0G, S0B, S1R, S1G, S1B, S2R, S2G, S2B, S3R, S3G, S3B, S4R, S4G, S4B;

    public GpuPaletteParams((double T, byte R, byte G, byte B)[] stops)
    {
        S0R = stops[0].R / 255f; S0G = stops[0].G / 255f; S0B = stops[0].B / 255f;
        S1R = stops[1].R / 255f; S1G = stops[1].G / 255f; S1B = stops[1].B / 255f;
        S2R = stops[2].R / 255f; S2G = stops[2].G / 255f; S2B = stops[2].B / 255f;
        S3R = stops[3].R / 255f; S3G = stops[3].G / 255f; S3B = stops[3].B / 255f;
        S4R = stops[4].R / 255f; S4G = stops[4].G / 255f; S4B = stops[4].B / 255f;
    }
}

/// <summary>
/// CUDA backend via ILGPU: one thread per pixel computes the escape, the smooth
/// coloring and the AA downsampling directly on the GPU.
/// </summary>
internal static class GpuMandelbrot
{
    private static Context? _context;
    private static Accelerator? _accelerator;
    private static Action<Index1D, ArrayView<int>, GpuViewParams, GpuPaletteParams>? _floatKernel;
    private static Action<Index1D, ArrayView<int>, GpuViewParams, GpuPaletteParams>? _doubleKernel;
    // Benchmark kernel: only the iteration count, no |z|² buffer (a third of the traffic).
    private static Action<Index1D, ArrayView<int>, GpuViewParams>? _floatBenchKernel;
    private static Action<Index1D, ArrayView<int>, GpuViewParams>? _doubleBenchKernel;
    private static readonly object RenderGate = new();
    private static MemoryBuffer1D<int, Stride1D.Dense>? _renderPixelsBuffer;
    private static int[]? _renderPixels;
    private static int _renderCount;

    public static bool IsReady => _accelerator != null;
    public static string DeviceName { get; private set; } = "";
    public static string DeviceShortName => DeviceName.Replace("NVIDIA GeForce ", "");
    /// <summary>Reason of the last failed initialization (diagnostic).</summary>
    public static string LastError { get; private set; } = "";

    /// <summary>True if the scale requires double (float does not have enough digits).</summary>
    public static bool WantsDouble(double scale) => scale < 1e-3;

    /// <summary>CUDA devices available (ILGPU names); empty if no CUDA.</summary>
    public static IReadOnlyList<string> DeviceNames()
    {
        try
        {
            _context ??= Context.Create(builder => builder.Default().EnableAlgorithms());
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
    /// Initializes the context, the CUDA device and the kernels. With <paramref name="deviceName"/>
    /// it uses that card; without it, it tries the CUDA devices from the largest.
    /// Returns false if none works (the CPU is used).
    /// </summary>
    public static bool TryInitialize(string? deviceName = null)
    {
        if (IsReady && (deviceName == null || DeviceName == deviceName)) return true;
        LastError = "";
        ResetAccelerator();
        try
        {
            _context ??= Context.Create(builder => builder.Default().EnableAlgorithms());
            var cudaDevices = _context.Devices
                .Where(d => d.AcceleratorType == AcceleratorType.Cuda)
                .ToList();
            if (cudaDevices.Count == 0)
            {
                LastError = "No CUDA device enumerated.";
                return false;
            }

            var candidates = deviceName == null
                ? cudaDevices.OrderByDescending(d => d.MemorySize).ToList()
                : cudaDevices.Where(d => d.Name == deviceName).ToList();
            if (candidates.Count == 0)
            {
                LastError = $"CUDA device not found: {deviceName}";
                return false;
            }

            foreach (var device in candidates)
            {
                try
                {
                    _accelerator = device.CreateAccelerator(_context);
                    DeviceName = device.Name;
                    _floatKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, GpuViewParams, GpuPaletteParams>(FloatKernel);
                    _doubleKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, GpuViewParams, GpuPaletteParams>(DoubleKernel);
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

    /// <summary>Unloads accelerator+kernels (the CUDA context stays reusable).</summary>
    private static void ResetAccelerator()
    {
        lock (RenderGate)
            ResetAcceleratorCore();
    }

    private static void ResetAcceleratorCore()
    {
        _floatKernel = null;
        _doubleKernel = null;
        _floatBenchKernel = null;
        _doubleBenchKernel = null;
        _renderPixelsBuffer?.Dispose();
        _renderPixelsBuffer = null;
        _renderPixels = null;
        _renderCount = 0;
        _accelerator?.Dispose();
        _accelerator = null;
        DeviceName = "";
    }

    /// <summary>Computes the frame on the GPU (kernel launch + copy back to RAM).</summary>
    /// <param name="supersample">Antialias: resolution k times greater (1 = none).</param>
    /// <param name="useDouble">True for the double 64-bit kernel, false for single 32-bit (float).</param>
    /// <param name="juliaCx">Constant c (real part) in Julia mode.</param>
    /// <param name="juliaCy">Constant c (imaginary part) in Julia mode.</param>
    /// <param name="julia">True = Julia set with fixed c, false = Mandelbrot.</param>
    public static bool Render(Bitmap bmp, double centerX, double centerY, double scale, int maxIter, Palette palette, int supersample, bool useDouble, CancellationToken ct, double juliaCx = 0, double juliaCy = 0, bool julia = false)
    {
        lock (RenderGate)
        {
            var accelerator = _accelerator ?? throw new InvalidOperationException("GPU not initialized.");
            int k = Math.Max(1, supersample);
            int bigW = bmp.Width * k;
            int bigH = bmp.Height * k;
            double pixelSize = scale / bigW;
            double topY = centerY - (bigH * 0.5) * pixelSize;
            var view = new GpuViewParams(centerX, centerY, pixelSize, topY, bmp.Width, bmp.Height, maxIter, k,
                julia ? 1 : 0, juliaCx, juliaCy);
            var paletteParams = new GpuPaletteParams(PaletteColors.GetStops(palette));
            int count = bmp.Width * bmp.Height;
            if (_renderCount != count)
            {
                _renderPixelsBuffer?.Dispose();
                _renderPixelsBuffer = accelerator.Allocate1D<int>(count);
                _renderPixels = new int[count];
                _renderCount = count;
            }

            var kernel = useDouble ? _doubleKernel! : _floatKernel!;
            var pixelsBuffer = _renderPixelsBuffer!;
            var pixels = _renderPixels!;
            kernel(count, pixelsBuffer.View, view, paletteParams);
            accelerator.Synchronize();
            ct.ThrowIfCancellationRequested();
            pixelsBuffer.CopyToCPU(pixels);

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return useDouble;
        }
    }

    /// The progress to the UI is limited (every 3 s) to not skew the measure.
    /// </summary>
    public static (long TotalIters, double Seconds, int Frames) BenchmarkGpu(double centerX, double centerY, double scale, int w, int h, int maxIter, int supersample, bool useDouble, TimeSpan budget, IProgress<BenchmarkProgress>? progress, CancellationToken ct)
    {
        var accelerator = _accelerator ?? throw new InvalidOperationException("GPU not initialized.");

        // Buffers allocated once and reused for all the frames (no extra alloc/copy).
        int k = Math.Max(1, supersample);
        int bigW = w * k;
        int bigH = h * k;
        double pixelSize = scale / bigW;
        double topY = centerY - (bigH * 0.5) * pixelSize;
        var pars = new GpuViewParams(centerX, centerY, pixelSize, topY, bigW, bigH, maxIter);
        int count = bigW * bigH;
        var kernel = useDouble ? _doubleBenchKernel! : _floatBenchKernel!;
        using var itersBuffer = accelerator.Allocate1D<int>(count);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        int frames = 0;
        bool first = true;
        TimeSpan lastReport = TimeSpan.Zero;

        while (sw.Elapsed < budget)
        {
            ct.ThrowIfCancellationRequested();
            kernel(count, itersBuffer.View, pars);
            accelerator.Synchronize();
            ct.ThrowIfCancellationRequested();
            frames++;
            if (first || sw.Elapsed - lastReport >= BenchmarkProgress.ReportInterval)
            {
                progress?.Report(new BenchmarkProgress(sw.Elapsed.TotalSeconds, 0, frames));
                lastReport = sw.Elapsed;
                first = false;
            }
        }

        progress?.Report(new BenchmarkProgress(sw.Elapsed.TotalSeconds, 0, frames));
        return (0, sw.Elapsed.TotalSeconds, frames);
    }

    public static void Dispose()
    {
        ResetAccelerator();
        _context?.Dispose();
        _context = null;
    }

    // ---------- Benchmark kernel: only iterations (no |z|², a third of the traffic) ----------

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

    /// <summary>
    /// Palette interpolation in the CUDA kernel: it must stay aligned with
    /// PaletteColors.ColorFor (CPU) and with the Graded function of the HLSL shader
    /// (DxMandelbrot.cs): same 5 stops and same mapping t = (nu/maxIter)^0.35.
    /// </summary>
    private static int ColorFromIterations(float iterations, int maxIter, GpuPaletteParams p)
    {
        float raw = iterations / (maxIter > 0 ? maxIter : 1);
        raw = raw < 0f ? 0f : raw > 1f ? 1f : raw;
        float t = MathF.Pow(raw, 0.35f);
        float segment = t * 4f;
        int i = (int)(segment < 3f ? segment : 3f);
        float f = segment - i;
        float ar = i == 0 ? p.S0R : i == 1 ? p.S1R : i == 2 ? p.S2R : p.S3R;
        float ag = i == 0 ? p.S0G : i == 1 ? p.S1G : i == 2 ? p.S2G : p.S3G;
        float ab = i == 0 ? p.S0B : i == 1 ? p.S1B : i == 2 ? p.S2B : p.S3B;
        float br = i == 0 ? p.S1R : i == 1 ? p.S2R : i == 2 ? p.S3R : p.S4R;
        float bg = i == 0 ? p.S1G : i == 1 ? p.S2G : i == 2 ? p.S3G : p.S4G;
        float bb = i == 0 ? p.S1B : i == 1 ? p.S2B : i == 2 ? p.S3B : p.S4B;
        return unchecked((int)(0xFF000000u | ((uint)((ar + f * (br - ar)) * 255f) << 16) | ((uint)((ag + f * (bg - ag)) * 255f) << 8) | (uint)((ab + f * (bb - ab)) * 255f)));
    }

    private static void FloatKernel(Index1D index, ArrayView<int> pixels, GpuViewParams p, GpuPaletteParams palette)
    {
        int i = index.X;
        int x = i % p.W;
        int y = i / p.W;
        int k = p.Supersample;
        float pixel = (float)p.PixelSize;
        float sumR = 0, sumG = 0, sumB = 0;
        for (int sy = 0; sy < k; sy++)
        {
            for (int sx = 0; sx < k; sx++)
            {
                float px = (float)p.CenterX + (x * k + sx - p.W * k * 0.5f) * pixel;
                float py = (float)p.TopY + (y * k + sy) * pixel;
                // Julia: z(0) = pixel point, c = constant; Mandelbrot: z(0) = 0, c = pixel.
                float zx = p.JuliaOn != 0 ? px : 0, zy = p.JuliaOn != 0 ? py : 0;
                float ccx = p.JuliaOn != 0 ? (float)p.Jcx : px;
                float ccy = p.JuliaOn != 0 ? (float)p.Jcy : py;
                float zx2 = zx * zx, zy2 = zy * zy;
                int iter = 0;
                while (iter < p.MaxIter && zx2 + zy2 <= 4f)
                {
                    zy = 2 * zx * zy + ccy;
                    zx = zx2 - zy2 + ccx;
                    zx2 = zx * zx;
                    zy2 = zy * zy;
                    ++iter;
                }
                float smoothIterations = iter >= p.MaxIter
                    ? p.MaxIter
                    : iter + 1f - XMath.Log2(0.5f * XMath.Log2(MathF.Max(zx2 + zy2, 4f)));
                int color = iter >= p.MaxIter
                    ? unchecked((int)0xFF000000)
                    : ColorFromIterations(smoothIterations, p.MaxIter, palette);
                sumR += (color >> 16) & 255;
                sumG += (color >> 8) & 255;
                sumB += color & 255;
            }
        }
        float samples = k * k;
        pixels[index] = unchecked((int)(0xFF000000u | ((uint)(sumR / samples) << 16) | ((uint)(sumG / samples) << 8) | (uint)(sumB / samples)));
    }

    private static void DoubleKernel(Index1D index, ArrayView<int> pixels, GpuViewParams p, GpuPaletteParams palette)
    {
        int i = index.X;
        int x = i % p.W;
        int y = i / p.W;
        int k = p.Supersample;
        double sumR = 0, sumG = 0, sumB = 0;
        for (int sy = 0; sy < k; sy++)
        {
            for (int sx = 0; sx < k; sx++)
            {
                double px = p.CenterX + (x * k + sx - p.W * k * 0.5) * p.PixelSize;
                double py = p.TopY + (y * k + sy) * p.PixelSize;
                // Julia: z(0) = pixel point, c = constant; Mandelbrot: z(0) = 0, c = pixel.
                double zx = p.JuliaOn != 0 ? px : 0, zy = p.JuliaOn != 0 ? py : 0;
                double ccx = p.JuliaOn != 0 ? p.Jcx : px;
                double ccy = p.JuliaOn != 0 ? p.Jcy : py;
                double zx2 = zx * zx, zy2 = zy * zy;
                int iter = 0;
                while (iter < p.MaxIter && zx2 + zy2 <= 4.0)
                {
                    zy = 2 * zx * zy + ccy;
                    zx = zx2 - zy2 + ccx;
                    zx2 = zx * zx;
                    zy2 = zy * zy;
                    ++iter;
                }
                double smoothIterations = iter >= p.MaxIter
                    ? p.MaxIter
                    : iter + 1.0 - XMath.Log2(0.5 * XMath.Log2(Math.Max(zx2 + zy2, 4.0)));
                int color = iter >= p.MaxIter
                    ? unchecked((int)0xFF000000)
                    : ColorFromIterations((float)smoothIterations, p.MaxIter, palette);
                sumR += (color >> 16) & 255;
                sumG += (color >> 8) & 255;
                sumB += color & 255;
            }
        }
        double samples = k * k;
        pixels[index] = unchecked((int)(0xFF000000u | ((uint)(sumR / samples) << 16) | ((uint)(sumG / samples) << 8) | (uint)(sumB / samples)));
    }
}
