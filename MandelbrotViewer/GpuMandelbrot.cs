using System.Drawing.Imaging;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

namespace MandelbrotViewer;

// View parameters passed to the kernels (blittable struct, must be public for ILGPU).
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
    public readonly int FullW;
    public readonly int FullH;
    public readonly int OffsetX;
    public readonly int OffsetY;

    public GpuViewParams(double centerX, double centerY, double pixelSize, double topY, int w, int h, int maxIter, int supersample = 1, int juliaOn = 0, double jcx = 0, double jcy = 0, int fullW = 0, int fullH = 0, int offsetX = 0, int offsetY = 0)
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
        FullW = fullW > 0 ? fullW : w;
        FullH = fullH > 0 ? fullH : h;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}

// CUDA backend via ILGPU: one thread per pixel computes the escape, the smooth
// coloring (through a cached device palette table shared with the CPU path)
// and the AA downsampling directly on the GPU.
internal static class GpuMandelbrot
{
    private static Context? _context;
    private static Accelerator? _accelerator;
    private static Action<Index1D, ArrayView<int>, GpuViewParams, ArrayView<int>>? _floatKernel;
    private static Action<Index1D, ArrayView<int>, GpuViewParams, ArrayView<int>>? _doubleKernel;
    // Benchmark kernel: only the iteration count, no |z|² buffer (a third of the traffic).
    private static Action<Index1D, ArrayView<int>, GpuViewParams>? _floatBenchKernel;
    private static Action<Index1D, ArrayView<int>, GpuViewParams>? _doubleBenchKernel;
    private static readonly object RenderGate = new();
    private static MemoryBuffer1D<int, Stride1D.Dense>? _renderPixelsBuffer;
    private static int[]? _renderPixels;
    private static int _renderCount;
    // Device palette table (packed ARGB, same entries as PaletteColors.GetLut),
    // uploaded once per palette/iteration combination and reused across frames.
    private static MemoryBuffer1D<int, Stride1D.Dense>? _lutBuffer;
    private static Palette _lutPalette = (Palette)(-1);
    private static int _lutMaxIter = -1;

    public static bool IsReady => _accelerator != null;
    public static string DeviceName { get; private set; } = "";
    public static string DeviceShortName => DeviceName.Replace("NVIDIA GeForce ", "");
    // Reason of the last failed initialization (diagnostic).
    public static string LastError { get; private set; } = "";

    // True if the scale requires double (float does not have enough digits).
    public static bool WantsDouble(double scale) => scale < 1e-3;

    // CUDA devices available (ILGPU names); empty if no CUDA.
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

    // Initializes the context, the CUDA device and the kernels. With deviceName
    // it uses that card; without it, it tries the CUDA devices from the largest.
    // Returns false if none works (the CPU is used).
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
                    _floatKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, GpuViewParams, ArrayView<int>>(FloatKernel);
                    _doubleKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, GpuViewParams, ArrayView<int>>(DoubleKernel);
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

    // Unloads accelerator+kernels (the CUDA context stays reusable).
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
        _lutBuffer?.Dispose();
        _lutBuffer = null;
        _lutPalette = (Palette)(-1);
        _lutMaxIter = -1;
        _accelerator?.Dispose();
        _accelerator = null;
        DeviceName = "";
    }

    // Ensures the device palette table matches the requested palette/iterations.
    // Uploads once per combination; frames reuse it. Must run under RenderGate.
    // Param accelerator (Accelerator): Input: active CUDA accelerator owning the buffer.
    // Param palette (Palette): Input: palette baked into the table.
    // Param maxIter (int): Input: iteration budget baked into the table normalization.
    private static void EnsureLut(Accelerator accelerator, Palette palette, int maxIter)
    {
        if (_lutBuffer != null && _lutPalette == palette && _lutMaxIter == maxIter)
            return;
        int[] host = PaletteColors.GetLut(palette, maxIter);
        _lutBuffer?.Dispose();
        _lutBuffer = accelerator.Allocate1D<int>(host.Length);
        _lutBuffer.CopyFromCPU(host);
        _lutPalette = palette;
        _lutMaxIter = maxIter;
    }

    // Computes the frame on the GPU (kernel launch + copy back to RAM).
    // Param supersample (int): Antialias: resolution k times greater (1 = none).
    // Param useDouble (bool): True for the double 64-bit kernel, false for single 32-bit (float).
    // Param juliaCx (double): Constant c (real part) in Julia mode.
    // Param juliaCy (double): Constant c (imaginary part) in Julia mode.
    // Param julia (bool): True = Julia set with fixed c, false = Mandelbrot.
    public static bool Render(Bitmap bmp, double centerX, double centerY, double scale, int maxIter, Palette palette, int supersample, bool useDouble, CancellationToken ct, double juliaCx = 0, double juliaCy = 0, bool julia = false)
    {
        return RenderTile(bmp, centerX, centerY, scale, maxIter, palette, supersample, useDouble, ct,
            0, 0, bmp.Width, bmp.Height, juliaCx, juliaCy, julia);
    }

    // Renders one output tile with global image coordinates.
    public static bool RenderTile(Bitmap bmp, double centerX, double centerY, double scale, int maxIter, Palette palette,
        int supersample, bool useDouble, CancellationToken ct, int offsetX, int offsetY, int fullWidth, int fullHeight,
        double juliaCx = 0, double juliaCy = 0, bool julia = false)
    {
        lock (RenderGate)
        {
            var accelerator = _accelerator ?? throw new InvalidOperationException("GPU not initialized.");
            int k = Math.Max(1, supersample);
            int bigW = fullWidth * k;
            int bigH = fullHeight * k;
            double pixelSize = scale / bigW;
            double topY = centerY - (bigH * 0.5) * pixelSize;
            var view = new GpuViewParams(centerX, centerY, pixelSize, topY, bmp.Width, bmp.Height, maxIter, k,
                julia ? 1 : 0, juliaCx, juliaCy, fullWidth, fullHeight, offsetX, offsetY);
            EnsureLut(accelerator, palette, maxIter);
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
            kernel(count, pixelsBuffer.View, view, _lutBuffer!.View);
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

    // The progress to the UI is limited (every 3 s) to not skew the measure.
    public static (long TotalIters, double Seconds, int Frames) BenchmarkGpu(double centerX, double centerY, double scale, int w, int h, int maxIter, int supersample, bool useDouble, TimeSpan budget, IProgress<BenchmarkProgress>? progress, CancellationToken ct)
    {
        lock (RenderGate)
            return BenchmarkGpuCore(centerX, centerY, scale, w, h, maxIter, supersample, useDouble, budget, progress, ct);
    }

    private static (long TotalIters, double Seconds, int Frames) BenchmarkGpuCore(double centerX, double centerY, double scale, int w, int h, int maxIter, int supersample, bool useDouble, TimeSpan budget, IProgress<BenchmarkProgress>? progress, CancellationToken ct)
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

        // Keep several kernels in flight, like the DirectX benchmark. A
        // synchronize after every launch measures submit latency and starves
        // fast GPUs instead of measuring their compute throughput.
        const int batchMin = 4;
        const int batchMax = 256;
        const double safeQueuedSeconds = 0.8;
        int estimateFrames = batchMin;
        var estimateWatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < estimateFrames; i++)
            kernel(count, itersBuffer.View, pars);
        accelerator.Synchronize();
        double frameSeconds = Math.Max(1e-6, estimateWatch.Elapsed.TotalSeconds / estimateFrames);
        int batchSize = Math.Clamp((int)Math.Round(safeQueuedSeconds / frameSeconds), batchMin, batchMax);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        int frames = 0;
        bool first = true;
        TimeSpan lastReport = TimeSpan.Zero;

        while (sw.Elapsed < budget)
        {
            ct.ThrowIfCancellationRequested();
            int submitted = 0;
            while (submitted < batchSize && sw.Elapsed < budget)
            {
                kernel(count, itersBuffer.View, pars);
                submitted++;
            }
            accelerator.Synchronize();
            ct.ThrowIfCancellationRequested();
            frames += submitted;
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

    // CUDA benchmark kernel, single precision (float 32-bit): escape-iteration
    // count of one elementary sample, no coloring, no smoothing, no SSAA averaging.
    // One GPU thread handles exactly one grid cell; the measured throughput is
    // pure Mandelbrot iteration compute.
    // Param index (Index1D): Input: linear thread index (0 … W*H-1). Decoded inside
    //   as x = index % W (column on the samples grid) and y = index / W (row).
    // Param iters (ArrayView<int>): Output: device buffer of W*H ints. Element [index] receives
    //   the escape-iteration count of this sample (0 … MaxIter). Read back by the host
    //   only for validation; the benchmark counts frames, not values.
    // Param p (GpuViewParams): Input: view parameters. Used here: W/H (grid size), CenterX,
    //   PixelSize, TopY (sample → complex mapping), MaxIter (loop bound). Ignored here:
    //   Supersample, palette, Julia fields, tile offsets (the benchmark grid is whole-frame).
    // Note: Coordinate mapping and incremental-squares loop are identical to the
    // DirectX benchmark shader, so the two engines measure the same workload.
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

    // CUDA benchmark kernel, double precision (float 64-bit): escape-iteration
    // count of one elementary sample, no coloring, no smoothing, no SSAA averaging.
    // One GPU thread handles exactly one grid cell; used when deep zoom needs more
    // digits than float provides (about 6x slower than the float kernel).
    // Param index (Index1D): Input: linear thread index (0 … W*H-1). Decoded inside
    //   as x = index % W (column on the samples grid) and y = index / W (row).
    // Param iters (ArrayView<int>): Output: device buffer of W*H ints. Element [index] receives
    //   the escape-iteration count of this sample (0 … MaxIter).
    // Param p (GpuViewParams): Input: view parameters. Used here: W/H (grid size), CenterX,
    //   CenterY-independent TopY, PixelSize (sample → complex mapping in double),
    //   MaxIter (loop bound). Ignored here: Supersample, palette, Julia fields,
    //   tile offsets.
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

    // Main cardioid + period-2 bulb test, device version of Mandelbrot.IsInteriorBulb.
    // Conservative in exact math (only true interior); the float kernel classifies
    // the float coordinates in double, like the CPU float render path.
    // Param px (double): Input: point real coordinate. Param py (double): Input: imaginary.
    // Returns (bool): Output: true when the point is known interior (renders black).
    private static bool IsInteriorBulbD(double px, double py)
    {
        double q = (px - 0.25) * (px - 0.25) + py * py;
        if (q * (q + (px - 0.25)) <= 0.25 * py * py) return true;
        double dx = px + 1.0;
        return dx * dx + py * py <= 0.0625;
    }

    // Palette lookup in the CUDA kernel through the cached device table (same
    // entries as PaletteColors.GetLut on the CPU): no Pow and no stop search per
    // subsample, linear interpolation between entries (no banding).
    // Device-only helper, called once per subsample by FloatKernel/DoubleKernel.
    // Param lut (ArrayView<int>): Input: device table of PaletteColors.LutSize packed
    //   ARGB colors (gamma baked). Read-only in the kernel.
    // Param smooth (float): Input: smoothed escape value nu. Interior points pass
    //   MaxIter and clamp to the last entry; exterior points carry the fractional
    //   log/log correction.
    // Param maxIter (int): Input: reference iteration budget of the view. Normalizes
    //   nu to raw = nu/maxIter; values outside [0,1] are clamped.
    // Returns (int): Output: packed 32-bit ARGB color (alpha always 0xFF) for one subsample;
    //   the caller accumulates its R/G/B channels into the pixel average.
    private static int LutColor(ArrayView<int> lut, float smooth, int maxIter)
    {
        int len = PaletteColors.LutSize;
        float pos = smooth / (maxIter > 0 ? maxIter : 1) * len - 0.5f;
        if (pos <= 0f) return lut[0];
        if (pos >= len - 1) return lut[len - 1];
        int i0 = (int)pos;
        float f = pos - i0;
        int c0 = lut[i0], c1 = lut[i0 + 1];
        float r = ((c0 >> 16) & 255) + f * ((((c1 >> 16) & 255) - ((c0 >> 16) & 255)));
        float g = ((c0 >> 8) & 255) + f * ((((c1 >> 8) & 255) - ((c0 >> 8) & 255)));
        float b = (c0 & 255) + f * (((c1 & 255) - (c0 & 255)));
        return unchecked((int)(0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | (uint)b));
    }

    // CUDA render kernel, single precision (float 32-bit): full on-chip SSAA color
    // of one output pixel. The thread loops over its k×k subsamples on the global
    // k-times grid, skips known-interior points via the cardioid/bulb test
    // (Mandelbrot only), runs the escape iteration with smooth log/log correction, maps
    // each subsample through the palette table, and writes the RGB average. VRAM traffic
    // stays O(W×H): the W*k×H*k grid is never materialized.
    // Param index (Index1D): Input: linear thread index (0 … W*H-1). Decoded inside
    //   as x = index % W (output column in this tile) and y = index / W (output row).
    // Param pixels (ArrayView<int>): Output: device buffer of W*H packed ARGB ints. Element [index]
    //   receives the SSAA-averaged color of this output pixel; the host copies it to RAM
    //   and blits it into the tile bitmap.
    // Param p (GpuViewParams): Input: view parameters. Used: W/H (tile size), Supersample k,
    //   FullW/FullH + OffsetX/OffsetY (tile → whole-image mapping, keeps tiled output
    //   pixel-identical), CenterX/TopY/PixelSize (grid → complex mapping in float),
    //   MaxIter, JuliaOn/Jcx/Jcy (Julia: z(0) = pixel, c = constant; else z(0) = 0, c = pixel).
    // Param lut (ArrayView<int>): Input: cached device palette table (packed ARGB).
    private static void FloatKernel(Index1D index, ArrayView<int> pixels, GpuViewParams p, ArrayView<int> lut)
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
                float px = (float)p.CenterX + ((p.OffsetX + x) * k + sx - p.FullW * k * 0.5f) * pixel;
                float py = (float)p.TopY + ((p.OffsetY + y) * k + sy) * pixel;
                int color;
                // Cardioid + period-2 bulb early-out (Mandelbrot only, mirrors the CPU):
                // interior points skip the escape loop entirely.
                if (p.JuliaOn == 0 && IsInteriorBulbD(px, py))
                {
                    color = unchecked((int)0xFF000000);
                }
                else
                {
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
                    color = iter >= p.MaxIter
                        ? unchecked((int)0xFF000000)
                        : LutColor(lut, smoothIterations, p.MaxIter);
                }
                sumR += (color >> 16) & 255;
                sumG += (color >> 8) & 255;
                sumB += color & 255;
            }
        }
        float samples = k * k;
        pixels[index] = unchecked((int)(0xFF000000u | ((uint)(sumR / samples) << 16) | ((uint)(sumG / samples) << 8) | (uint)(sumB / samples)));
    }

    // CUDA render kernel, double precision (float 64-bit): full on-chip SSAA color
    // of one output pixel. Same contract as FloatKernel (including the
    // cardioid/bulb early-out), but the escape iteration
    // and the grid → complex mapping run in double, so deep zoom (scale &lt; 1e-3)
    // stays sharp where float runs out of digits. About 6x slower than float.
    // Param index (Index1D): Input: linear thread index (0 … W*H-1). Decoded inside
    //   as x = index % W (output column in this tile) and y = index / W (output row).
    // Param pixels (ArrayView<int>): Output: device buffer of W*H packed ARGB ints. Element [index]
    //   receives the SSAA-averaged color of this output pixel.
    // Param p (GpuViewParams): Input: view parameters. Used: W/H (tile size), Supersample k,
    //   FullW/FullH + OffsetX/OffsetY (tile → whole-image mapping), CenterX/TopY/PixelSize
    //   (grid → complex mapping in double), MaxIter, JuliaOn/Jcx/Jcy (mode switch).
    // Param lut (ArrayView<int>): Input: cached device palette table (packed ARGB;
    //   the smoothed value is narrowed to float only for the table lookup).
    private static void DoubleKernel(Index1D index, ArrayView<int> pixels, GpuViewParams p, ArrayView<int> lut)
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
                double px = p.CenterX + ((p.OffsetX + x) * k + sx - p.FullW * k * 0.5) * p.PixelSize;
                double py = p.TopY + ((p.OffsetY + y) * k + sy) * p.PixelSize;
                int color;
                // Cardioid + period-2 bulb early-out (Mandelbrot only, mirrors the CPU):
                // interior points skip the escape loop entirely.
                if (p.JuliaOn == 0 && IsInteriorBulbD(px, py))
                {
                    color = unchecked((int)0xFF000000);
                }
                else
                {
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
                    color = iter >= p.MaxIter
                        ? unchecked((int)0xFF000000)
                        : LutColor(lut, (float)smoothIterations, p.MaxIter);
                }
                sumR += (color >> 16) & 255;
                sumG += (color >> 8) & 255;
                sumB += color & 255;
            }
        }
        double samples = k * k;
        pixels[index] = unchecked((int)(0xFF000000u | ((uint)(sumR / samples) << 16) | ((uint)(sumG / samples) << 8) | (uint)(sumB / samples)));
    }
}
