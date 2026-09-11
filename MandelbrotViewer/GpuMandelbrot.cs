using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MandelbrotViewer;

// View parameters passed to the kernels (blittable struct shared with
// Cuda/mandelbrot.cu: same field order, same 8-byte packing, 88 bytes total).
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

// CUDA backend over the driver API (nvcuda.dll): kernels are compiled from
// Cuda/mandelbrot.cu to PTX at build time and launched with an explicit
// grid/block layout chosen here (no runtime compiler, no JIT framework).
// One thread per pixel computes the escape, the smooth coloring (through a
// cached device palette table shared with the CPU path) and the AA
// downsampling directly on the GPU.
internal static class GpuMandelbrot
{
    // Embedded PTX resource built from Cuda/mandelbrot.cu (see csproj target).
    private const string PtxResourceName = "MandelbrotViewer.Cuda.mandelbrot.ptx";
    // Expected native size of GpuViewParams (must match the C struct).
    private const int ViewParamsSize = 88;
    // Block-size candidates swept by the init probe (threads per block).
    private static readonly int[] TuneCandidates = [128, 256, 512, 1024];

    private static readonly object RenderGate = new();
    private static IntPtr _ctx = IntPtr.Zero;
    private static IntPtr _module = IntPtr.Zero;
    private static IntPtr _floatFunc = IntPtr.Zero;
    private static IntPtr _doubleFunc = IntPtr.Zero;
    private static IntPtr _floatBenchFunc = IntPtr.Zero;
    private static IntPtr _doubleBenchFunc = IntPtr.Zero;
    private static ulong _dPixels;
    private static int[]? _renderPixels;
    private static int _renderCount;
    // Device palette table (packed ARGB, same entries as PaletteColors.GetLut),
    // uploaded once per palette/iteration combination and reused across frames.
    private static ulong _dLut;
    private static Palette _lutPalette = (Palette)(-1);
    private static int _lutMaxIter = -1;
    // Explicit wave allocation: tuned threads-per-block per precision
    // (default 256 = 8 warps; refined by the init probe, see TuneBlocks).
    private static int _blockFloat = 256;
    private static int _blockDouble = 256;

    public static bool IsReady => _ctx != IntPtr.Zero;
    public static string DeviceName { get; private set; } = "";
    public static string DeviceShortName => DeviceName.Replace("NVIDIA GeForce ", "");
    // Reason of the last failed initialization (diagnostic).
    public static string LastError { get; private set; } = "";
    // Device summary with the chosen launch layout (diagnostic).
    public static string DeviceDetails { get; private set; } = "";

    // True if the scale requires double (float does not have enough digits).
    public static bool WantsDouble(double scale) => scale < 1e-3;

    // CUDA devices available (driver names); empty if no CUDA.
    public static IReadOnlyList<string> DeviceNames()
    {
        try
        {
            CudaNative.Init();
            int n = CudaNative.DeviceCount();
            var names = new List<string>(n);
            for (int o = 0; o < n; o++)
            {
                try
                {
                    names.Add(CudaNative.GetDeviceName(CudaNative.GetDevice(o)));
                }
                catch
                {
                    // Skip unreadable devices, like the old enumeration did.
                }
            }
            return names.Distinct().OrderBy(x => x).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    // Initializes the driver, the CUDA context and the kernels. With deviceName
    // it uses that card; without it, it tries the CUDA devices from the largest.
    // Returns false if none works (the CPU is used).
    public static bool TryInitialize(string? deviceName = null)
    {
        if (IsReady && (deviceName == null || DeviceName == deviceName)) return true;
        LastError = "";
        ResetCore();
        try
        {
            if (Marshal.SizeOf<GpuViewParams>() != ViewParamsSize)
                throw new InvalidOperationException(
                    $"GpuViewParams is {Marshal.SizeOf<GpuViewParams>()} bytes, native side expects {ViewParamsSize}.");
        }
        catch (Exception ex)
        {
            LastError = ex.GetType().Name + ": " + ex.Message;
            return false;
        }
        byte[] ptx;
        try
        {
            ptx = LoadPtxImage();
        }
        catch (Exception ex)
        {
            LastError = ex.GetType().Name + ": " + ex.Message;
            return false;
        }
        List<(int Dev, string Name, ulong Mem)> devices;
        try
        {
            CudaNative.Init();
            int n = CudaNative.DeviceCount();
            devices = new List<(int, string, ulong)>(n);
            for (int o = 0; o < n; o++)
            {
                int dev;
                try
                {
                    dev = CudaNative.GetDevice(o);
                }
                catch
                {
                    continue;
                }
                string name;
                try
                {
                    name = CudaNative.GetDeviceName(dev);
                }
                catch
                {
                    name = $"CUDA device {o}";
                }
                ulong mem;
                try
                {
                    mem = CudaNative.GetTotalMem(dev);
                }
                catch
                {
                    mem = 0;
                }
                devices.Add((dev, name, mem));
            }
        }
        catch (DllNotFoundException ex)
        {
            LastError = "NVIDIA driver (nvcuda.dll) not available: " + ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            LastError = ex.GetType().Name + ": " + ex.Message;
            return false;
        }
        if (devices.Count == 0)
        {
            LastError = "No CUDA device enumerated.";
            return false;
        }

        var candidates = deviceName == null
            ? devices.OrderByDescending(d => d.Mem).ToList()
            : devices.Where(d => d.Name == deviceName).ToList();
        if (candidates.Count == 0)
        {
            LastError = $"CUDA device not found: {deviceName}";
            return false;
        }

        foreach (var c in candidates)
        {
            try
            {
                SetupDevice(c.Dev, c.Name, c.Mem, ptx);
                return true;
            }
            catch (Exception ex)
            {
                LastError = $"{c.Name}: {ex.GetType().Name}: {ex.Message}";
                ResetCore();
            }
        }
        return false;
    }

    // Reads the embedded PTX image with a trailing zero byte for cuModuleLoadDataEx.
    // Returns (byte[]): Output: PTX bytes plus one zero terminator.
    private static byte[] LoadPtxImage()
    {
        using var s = typeof(GpuMandelbrot).Assembly.GetManifestResourceStream(PtxResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded CUDA PTX '{PtxResourceName}' not found (rebuild with the PTX resource).");
        if (s.Length is <= 0 or > 10_000_000)
            throw new InvalidOperationException($"Embedded CUDA PTX has an unexpected size ({s.Length} bytes).");
        var ptx = new byte[s.Length + 1];
        int read = s.Read(ptx, 0, (int)s.Length);
        if (read != (int)s.Length)
            throw new InvalidOperationException("Embedded CUDA PTX could not be fully read.");
        ptx[^1] = 0;
        return ptx;
    }

    // Creates the context on one device, loads the module and tunes the launch
    // layout. Must run outside RenderGate (caller holds no lock yet).
    // Param dev (int): Input: driver device handle. Param name (string): Input: device display name.
    // Param mem (ulong): Input: total device memory in bytes. Param ptx (byte[]): Input: PTX image.
    private static void SetupDevice(int dev, string name, ulong mem, byte[] ptx)
    {
        _ctx = CudaNative.CreateContext(dev);
        try
        {
            _module = CudaNative.LoadModule(ptx);
            _floatFunc = CudaNative.GetFunction(_module, "FloatKernel");
            _doubleFunc = CudaNative.GetFunction(_module, "DoubleKernel");
            _floatBenchFunc = CudaNative.GetFunction(_module, "FloatBenchKernel");
            _doubleBenchFunc = CudaNative.GetFunction(_module, "DoubleBenchKernel");
            DeviceName = name;
            int sm = CudaNative.GetAttribute(dev, CudaNative.AttrMultiprocessorCount);
            int major = CudaNative.GetAttribute(dev, CudaNative.AttrComputeCapabilityMajor);
            int minor = CudaNative.GetAttribute(dev, CudaNative.AttrComputeCapabilityMinor);
            TuneBlocks();
            int occF = CudaNative.ActiveBlocksPerSm(_floatBenchFunc, _blockFloat);
            int occD = CudaNative.ActiveBlocksPerSm(_doubleBenchFunc, _blockDouble);
            DeviceDetails = $"SM x{sm}, CC {major}.{minor}, {mem / (1024 * 1024)} MB, " +
                $"block {_blockFloat}/{_blockDouble} (32/64-bit), occ {occF}/{occD} blocks/SM";
        }
        catch
        {
            ResetCore();
            throw;
        }
    }

    // Picks the fastest threads-per-block per precision with a small occupancy
    // probe (480x270 grid, 500 iterations): for this register-light escape loop
    // the ranking is occupancy-driven, so it holds for full-size frames while
    // the probe costs a fraction of a second even in double precision.
    // Wave math for a frame of N threads on S SMs with B threads/block and
    // R resident blocks/SM: blocks = ceil(N/B), waves = blocks/(S*R).
    // Must run with the context current on this thread.
    private static void TuneBlocks()
    {
        const int pw = 480;
        const int ph = 270;
        const int pIter = 500;
        double pixel = 0.01 / pw;
        var probe = new GpuViewParams(-0.75, 0.0, pixel, 0.0 - (ph * 0.5) * pixel, pw, ph, pIter);
        int count = pw * ph;
        ulong dProbe = CudaNative.Alloc((ulong)count * 4);
        try
        {
            _blockFloat = TuneOne(_floatBenchFunc, dProbe, probe, count);
            _blockDouble = TuneOne(_doubleBenchFunc, dProbe, probe, count);
        }
        finally
        {
            CudaNative.Free(dProbe);
        }
    }

    // Times one kernel over the candidate block sizes, returns the fastest.
    // Param func (IntPtr): Input: bench kernel handle. Param dBuf (ulong): Input: probe device buffer.
    // Param view (GpuViewParams): Input: probe view. Param count (int): Input: probe thread count.
    // Returns (int): Output: winning threads-per-block (256 on any failure).
    private static int TuneOne(IntPtr func, ulong dBuf, in GpuViewParams view, int count)
    {
        int best = 256;
        double bestSeconds = double.PositiveInfinity;
        foreach (int b in TuneCandidates)
        {
            try
            {
                uint grid = GridFor(count, b);
                CudaNative.LaunchBench(func, grid, (uint)b, dBuf, view);
                CudaNative.Synchronize();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 3; i++)
                    CudaNative.LaunchBench(func, grid, (uint)b, dBuf, view);
                CudaNative.Synchronize();
                sw.Stop();
                double avg = sw.Elapsed.TotalSeconds / 3;
                if (avg < bestSeconds)
                {
                    bestSeconds = avg;
                    best = b;
                }
            }
            catch
            {
                // A failing candidate is skipped; the default survives.
            }
        }
        return best;
    }

    // Block count covering count threads. Param count (int): Input: thread count.
    // Param block (int): Input: threads per block. Returns (uint): Output: grid size.
    private static uint GridFor(int count, int block) => (uint)((count + block - 1) / block);

    // Unloads context+kernels and frees device buffers (the driver stays initialized).
    private static void ResetCore()
    {
        lock (RenderGate)
        {
            _floatFunc = IntPtr.Zero;
            _doubleFunc = IntPtr.Zero;
            _floatBenchFunc = IntPtr.Zero;
            _doubleBenchFunc = IntPtr.Zero;
            if (_dPixels != 0)
            {
                try
                {
                    CudaNative.Free(_dPixels);
                }
                catch
                {
                    // Best effort during teardown.
                }
                _dPixels = 0;
            }
            if (_dLut != 0)
            {
                try
                {
                    CudaNative.Free(_dLut);
                }
                catch
                {
                    // Best effort during teardown.
                }
                _dLut = 0;
            }
            _renderPixels = null;
            _renderCount = 0;
            _lutPalette = (Palette)(-1);
            _lutMaxIter = -1;
            if (_module != IntPtr.Zero)
            {
                try
                {
                    CudaNative.UnloadModule(_module);
                }
                catch
                {
                    // Best effort during teardown.
                }
                _module = IntPtr.Zero;
            }
            if (_ctx != IntPtr.Zero)
            {
                try
                {
                    CudaNative.DestroyContext(_ctx);
                }
                catch
                {
                    // Best effort during teardown.
                }
                _ctx = IntPtr.Zero;
            }
            DeviceName = "";
            DeviceDetails = "";
            _blockFloat = 256;
            _blockDouble = 256;
        }
    }

    // Ensures the device palette table matches the requested palette/iterations.
    // Uploads once per combination; frames reuse it. Must run under RenderGate
    // with the context current.
    // Param palette (Palette): Input: palette baked into the table.
    // Param maxIter (int): Input: iteration budget baked into the table normalization.
    private static void EnsureLut(Palette palette, int maxIter)
    {
        if (_dLut != 0 && _lutPalette == palette && _lutMaxIter == maxIter)
            return;
        int[] host = PaletteColors.GetLut(palette, maxIter);
        if (_dLut != 0)
        {
            CudaNative.Free(_dLut);
            _dLut = 0;
        }
        _dLut = CudaNative.Alloc((ulong)host.Length * 4);
        CudaNative.CopyHtoD(_dLut, host);
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
            if (_ctx == IntPtr.Zero) throw new InvalidOperationException("GPU not initialized.");
            CudaNative.SetCurrent(_ctx);
            int k = Math.Max(1, supersample);
            int bigW = fullWidth * k;
            int bigH = fullHeight * k;
            double pixelSize = scale / bigW;
            double topY = centerY - (bigH * 0.5) * pixelSize;
            var view = new GpuViewParams(centerX, centerY, pixelSize, topY, bmp.Width, bmp.Height, maxIter, k,
                julia ? 1 : 0, juliaCx, juliaCy, fullWidth, fullHeight, offsetX, offsetY);
            EnsureLut(palette, maxIter);
            int count = bmp.Width * bmp.Height;
            if (_renderCount != count)
            {
                if (_dPixels != 0)
                {
                    CudaNative.Free(_dPixels);
                    _dPixels = 0;
                }
                _dPixels = CudaNative.Alloc((ulong)count * 4);
                _renderPixels = new int[count];
                _renderCount = count;
            }

            int block = useDouble ? _blockDouble : _blockFloat;
            IntPtr func = useDouble ? _doubleFunc : _floatFunc;
            CudaNative.LaunchRender(func, GridFor(count, block), (uint)block, _dPixels, view, _dLut);
            CudaNative.Synchronize();
            ct.ThrowIfCancellationRequested();
            var pixels = _renderPixels!;
            CudaNative.CopyDtoH(pixels, _dPixels);

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
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
        if (_ctx == IntPtr.Zero) throw new InvalidOperationException("GPU not initialized.");
        CudaNative.SetCurrent(_ctx);

        // Buffers allocated once and reused for all the frames (no extra alloc/copy).
        int k = Math.Max(1, supersample);
        int bigW = w * k;
        int bigH = h * k;
        double pixelSize = scale / bigW;
        double topY = centerY - (bigH * 0.5) * pixelSize;
        var pars = new GpuViewParams(centerX, centerY, pixelSize, topY, bigW, bigH, maxIter);
        int count = bigW * bigH;
        int block = useDouble ? _blockDouble : _blockFloat;
        IntPtr func = useDouble ? _doubleBenchFunc : _floatBenchFunc;
        uint grid = GridFor(count, block);
        ulong dIters = CudaNative.Alloc((ulong)count * 4);
        try
        {
            // Keep several kernels in flight, like the DirectX benchmark. A
            // synchronize after every launch measures submit latency and starves
            // fast GPUs instead of measuring their compute throughput.
            const int batchMin = 4;
            const int batchMax = 256;
            const double safeQueuedSeconds = 0.8;
            int estimateFrames = batchMin;
            var estimateWatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < estimateFrames; i++)
                CudaNative.LaunchBench(func, grid, (uint)block, dIters, pars);
            CudaNative.Synchronize();
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
                    CudaNative.LaunchBench(func, grid, (uint)block, dIters, pars);
                    submitted++;
                }
                CudaNative.Synchronize();
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
        finally
        {
            CudaNative.Free(dIters);
        }
    }

    public static void Dispose()
    {
        ResetCore();
    }
}
