using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.D3DCompiler;
using Vortice.Mathematics;
using DxgiFormat = Vortice.DXGI.Format;

namespace MandelbrotViewer;

/// <summary>
/// Realtime DirectX 11 backend: fullscreen triangle + HLSL pixel shader that computes
/// the fractal every frame (float). Presents to a swapchain bound to the panel's handle.
/// </summary>
internal static class DxMandelbrot
{
    // Fullscreen triangle without a vertex buffer (SV_VertexID).
    private const string VsSource = @"
float4 VS(uint id : SV_VertexID) : SV_Position
{
    float2 p = float2((id << 1) & 2, id & 2);
    return float4(p * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
}";

    private const string PsSource = @"
cbuffer Params : register(b0)
{
    float cx; float cy; float scale; float aspect;
    float invW; float invH; int maxIter; int aa;
    float jcx; float jcy; int julia; int jpad;
    float3 stops[5];
};

float3 Graded(float t)
{
    t = saturate(t);
    float seg = t * 4.0;
    int i = (int)min(seg, 3.0);
    float f = seg - (float)i;
    return lerp(stops[i], stops[i + 1], f);
}

float4 PS(float4 pos : SV_Position) : SV_Target
{
    int n = max(aa, 1);
    float3 acc = 0.0;
    for (int jy = 0; jy < n; jy++)
    {
        for (int jx = 0; jx < n; jx++)
        {
            float2 sub = (float2((float)jx, (float)jy) + 0.5) / (float)n - 0.5;
            float2 p = pos.xy + sub;
            float2 px = float2(cx + (p.x * invW - 0.5) * scale,
                               cy + (p.y * invH - 0.5) * scale * aspect);
            // Julia: z(0) = pixel point, c = constant; Mandelbrot: z(0) = 0, c = pixel.
            float2 z = julia != 0 ? px : 0.0;
            float2 cc = julia != 0 ? float2(jcx, jcy) : px;
            int iter = 0;
            while (iter < maxIter && dot(z, z) <= 4.0)
            {
                z = float2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + cc;
                iter++;
            }
            float3 col = 0.0;
            if (iter < maxIter)
            {
                float mod2 = max(dot(z, z), 4.0);
                float smoothIterations = (float)iter + 1.0 - log(log(sqrt(mod2))) / log(2.0);
                // Mapping aligned with PaletteColors (CPU) and GpuMandelbrot.ColorFromIterations (CUDA).
                float t = pow(saturate(smoothIterations / (float)maxIter), 0.35);
                col = Graded(t);
            }
            acc += col;
        }
    }
    return float4(acc / (float)(n * n), 1.0);
}";

    /// <summary>
    /// Benchmark shader: only the iteration count of the fractal, no
    /// coloring, no smooth and no sample averaging. Each pixel of the
    /// grid is an elementary sample: identical to the work of the CUDA/CPU kernels.
    /// </summary>
    private const string BenchPsSource = @"
cbuffer Params : register(b0)
{
    float cx; float cy; float scale; float aspect;
    float invW; float invH; int maxIter; int aa;
};

float4 BenchPS(float4 pos : SV_Position) : SV_Target
{
    float2 c = float2(cx + (pos.x * invW - 0.5) * scale,
                        cy + (pos.y * invH - 0.5) * scale * aspect);
    float2 z = 0.0;
    int iter = 0;
    while (iter < maxIter && dot(z, z) <= 4.0)
    {
        z = float2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + c;
        iter++;
    }
    // The output depends on the iterations: the compiler cannot eliminate the loop.
    float v = iter >= maxIter ? 0.0 : frac(float(iter) * 0.125);
    return float4(v, v, v, 1.0);
}
";

    [StructLayout(LayoutKind.Sequential)]
    private struct DxStop
    {
        public float R, G, B, Pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DxParams
    {
        public float Cx, Cy, Scale, Aspect;
        public float InvW, InvH;
        public int MaxIter, Aa;
        public float JuliaCx, JuliaCy;
        public int JuliaOn, JuliaPad;
        public DxStop S0, S1, S2, S3, S4;
    }

    private static ID3D11Device? _device;
    private static ID3D11DeviceContext? _context;
    private static IDXGISwapChain1? _swapChain;
    private static ID3D11RenderTargetView? _rtv;
    private static ID3D11VertexShader? _vs;
    private static ID3D11PixelShader? _ps;
    private static ID3D11Buffer? _cbuffer;
    private static ID3D11PixelShader? _benchPs;
    private static int _backWidth, _backHeight;

    // Offscreen benchmark (v2.5.20): render target in the tested GPU's memory,
    // no swapchain and no Present — no DWM, no cross-GPU copy to the
    // monitor's card (which skewed the headless cards, see SPECIFICHE).
    // Draw returns immediately (commands queued): frame completion is detected
    // with a ring of event queries.
    private static ID3D11Texture2D? _benchTarget;
    private static ID3D11RenderTargetView? _benchRtv;
    private static ID3D11Query?[] _benchQueries = Array.Empty<ID3D11Query?>();
    private static int _benchGridW, _benchGridH;

    public static bool IsReady => _device != null;
    public static string LastError { get; private set; } = "";
    /// <summary>Name of the video card currently in use ("" if not initialized).</summary>
    public static string AdapterName { get; private set; } = "";

    /// <summary>Dedicated + shared memory of the adapter in use (ulong.MaxValue if unknown).</summary>
    public static ulong AdapterDedicatedBytes { get; private set; } = ulong.MaxValue;
    public static ulong AdapterSharedBytes { get; private set; } = ulong.MaxValue;

    /// <summary>Error of the last failed enumeration (diagnostic), empty if OK.</summary>
    public static string EnumerationError { get; private set; } = "";

    /// <summary>Available hardware video cards (software WARP renderers excluded).</summary>
    /// <remarks>
    /// Do NOT read or compare DedicatedVideoMemory directly: it is a
    /// PointerUSize (SIZE_T) and the implicit conversion of SharpGen goes through 32 bits
    /// (UIntPtr.ToUInt32), which throws OverflowException with GPUs of more than 4 GB — that is
    /// why the enumeration stayed empty on machines with modern GPUs
    /// (cf. note v2.3.8 "wrapper overflow"). The software renderers are
    /// excluded by name: "Microsoft Basic Render Driver" is the WARP of D3D11.
    /// </remarks>
    public static IReadOnlyList<string> AdapterNames()
    {
        var names = new List<string>();
        EnumerationError = "";
        try
        {
            using IDXGIFactory1 factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            for (uint i = 0; i < 32; i++)
            {
                if (factory.EnumAdapters(i, out IDXGIAdapter adapter).Failure) break;
                using (adapter)
                {
                    string name = adapter.Description.Description;
                    if (!name.StartsWith("Microsoft Basic", StringComparison.OrdinalIgnoreCase))
                        names.Add(name);
                }
            }
        }
        catch (Exception ex)
        {
            EnumerationError = $"{ex.GetType().Name}: {ex.Message}";
        }
        return names;
    }

    /// <summary>Reads the memory of the chosen adapter (or of the first hardware one if auto).
    /// DXGI sizes above 4 GB overflow the wrapper's 32-bit conversion:
    /// in that case (or if the adapter is not found) it stays ulong.MaxValue
    /// = abundant/unknown memory and the VRAM check is skipped.</summary>
    private static void RefreshAdapterMemory(string? adapterName)
    {
        AdapterDedicatedBytes = ulong.MaxValue;
        AdapterSharedBytes = ulong.MaxValue;
        try
        {
            using IDXGIFactory1 factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            for (uint i = 0; i < 32; i++)
            {
                if (factory.EnumAdapters(i, out IDXGIAdapter adapter).Failure) break;
                using (adapter)
                {
                    string name = adapter.Description.Description;
                    if (name.StartsWith("Microsoft Basic", StringComparison.OrdinalIgnoreCase)) continue;
                    if (adapterName != null &&
                        !string.Equals(name, adapterName, StringComparison.OrdinalIgnoreCase)) continue;
                    var d = adapter.Description;
                    AdapterDedicatedBytes = SafeMemBytes(() => (ulong)(d.DedicatedVideoMemory / (1024 * 1024)) * 1024ul * 1024ul);
                    AdapterSharedBytes = SafeMemBytes(() => (ulong)(d.SharedSystemMemory / (1024 * 1024)) * 1024ul * 1024ul);
                    return;
                }
            }
        }
        catch { /* unknown memory: no check */ }
    }

    private static ulong SafeMemBytes(Func<ulong> read)
    {
        try { return read(); }
        catch { return ulong.MaxValue; }
    }

    /// <param name="adapterName">Card to use (exact DXGI name); null = default hardware adapter.
    /// With a requested card, the device is created explicitly on the chosen adapter.</param>
    public static bool TryInitialize(IntPtr hwnd, int width, int height, string? adapterName = null)
    {
        if (!EnsureDevice(adapterName))
            return false;
        LastError = "";
        string step = "start";
        try
        {
            step = "DXGI.CreateDXGIFactory2";
            using IDXGIFactory2 factory = DXGI.CreateDXGIFactory2<IDXGIFactory2>(false);

            width = Math.Max(1, width);
            height = Math.Max(1, height);
            var desc = new SwapChainDescription1
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = DxgiFormat.R8G8B8A8_UNorm,
                BufferCount = 2,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                AlphaMode = AlphaMode.Ignore,
            };
            var fullscreen = new SwapChainFullscreenDescription { Windowed = true };
            step = "IDXGIFactory2.CreateSwapChainForHwnd";
            _swapChain = factory.CreateSwapChainForHwnd(_device!, hwnd, desc, fullscreen);

            step = "CreateViews (render target)";
            CreateViews(width, height);
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"[{step}] {ex.GetType().Name}: {ex.Message}";
            Dispose();
            return false;
        }
    }

    /// <summary>
    /// Initializes device, context and shaders on the chosen card without a window or a
    /// swapchain (headless): for the offscreen benchmark, which presents nothing.
    /// </summary>
    /// <param name="adapterName">Card to use (exact DXGI name); null = default.</param>
    public static bool TryInitializeHeadless(string? adapterName = null) =>
        EnsureDevice(adapterName);

    /// <summary>
    /// Creates (or reuses) device, context and shaders on the chosen adapter. The swapchain
    /// stays the responsibility of <see cref="TryInitialize"/> (it needs a window).
    /// </summary>
    private static bool EnsureDevice(string? adapterName)
    {
        bool same = adapterName == null ||
            string.Equals(AdapterName, adapterName, StringComparison.OrdinalIgnoreCase);
        if (IsReady)
        {
            if (same) return true;
            Dispose(); // card change: the device must be recreated on the chosen adapter
        }
        LastError = "";
        string step = "start";
        try
        {
            step = "DXGI.CreateDXGIFactory2";
            using IDXGIFactory2 factory = DXGI.CreateDXGIFactory2<IDXGIFactory2>(false);

            // If the user chose a card, find the matching DXGI adapter
            // and pass it explicitly to D3D11CreateDevice (with DriverType.Unknown, as
            // required when passing an adapter); otherwise the default adapter.
            IDXGIAdapter? chosen = null;
            if (adapterName != null)
            {
                step = "searching for the requested DXGI adapter";
                for (uint i = 0; i < 32; i++)
                {
                    if (factory.EnumAdapters(i, out IDXGIAdapter adapter).Failure) break;
                    if (string.Equals(adapter.Description.Description, adapterName, StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = adapter; // the dispose happens after the device creation
                        break;
                    }
                    adapter.Dispose();
                }
                if (chosen == null)
                    throw new InvalidOperationException($"Video card not found: {adapterName}");
            }

            step = chosen != null
                ? $"D3D11CreateDevice (chosen adapter: {adapterName})"
                : "D3D11CreateDevice (default hardware adapter)";
            var result = D3D11.D3D11CreateDevice(
                chosen, chosen != null ? DriverType.Unknown : DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_0 },
                out ID3D11Device device,
                out FeatureLevel level,
                out ID3D11DeviceContext context);
            chosen?.Dispose(); // the device keeps its own reference to the adapter
            if (result.Failure)
                throw new InvalidOperationException($"D3D11CreateDevice failed (HRESULT 0x{result.Code:X8})");
            _device = device;
            _context = context;
            AdapterName = adapterName ?? "Auto (default hardware adapter)";
            RefreshAdapterMemory(adapterName);

            step = "D3DCompiler.Compile (shaders)";
            ReadOnlyMemory<byte> vsCode = Compiler.Compile(VsSource, "VS", "mandelbrot-vs", "vs_5_0");
            ReadOnlyMemory<byte> psCode = Compiler.Compile(PsSource, "PS", "mandelbrot-ps", "ps_5_0");
            _vs = _device.CreateVertexShader(vsCode.Span);
            _ps = _device.CreatePixelShader(psCode.Span);

            step = "D3DCompiler.Compile (benchmark shader)";
            ReadOnlyMemory<byte> benchCode = Compiler.Compile(BenchPsSource, "BenchPS", "mandelbrot-bench-ps", "ps_5_0");
            _benchPs = _device.CreatePixelShader(benchCode.Span);

            step = "CreateBuffer (constant buffer)";
            _cbuffer = _device.CreateBuffer(
                (uint)Marshal.SizeOf<DxParams>(),
                BindFlags.ConstantBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write);

            return true;
        }
        catch (Exception ex)
        {
            LastError = $"[{step}] {ex.GetType().Name}: {ex.Message}";
            Dispose();
            return false;
        }
    }

    private static void CreateViews(int width, int height)
    {
        _backWidth = width;
        _backHeight = height;
        _rtv?.Dispose();
        using ID3D11Texture2D backbuffer = _swapChain!.GetBuffer<ID3D11Texture2D>(0);
        _rtv = _device!.CreateRenderTargetView(backbuffer);
        _context!.RSSetViewport(new Viewport(width, height));
    }

    public static void Resize(int width, int height)
    {
        if (!IsReady) return;
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (width == _backWidth && height == _backHeight) return;
        _rtv?.Dispose();
        _rtv = null;
        _swapChain!.ResizeBuffers(0, (uint)width, (uint)height, DxgiFormat.Unknown, SwapChainFlags.None);
        CreateViews(width, height);
    }

    /// <summary>Builds the parameters of a colored frame (with the palette stops).</summary>
    private static DxParams BuildParams(double centerX, double centerY, double scale, int width, int height, int maxIter, int aa, Palette palette, double juliaCx = 0, double juliaCy = 0, bool julia = false)
    {
        var stops = PaletteColors.GetStops(palette);
        return new DxParams
        {
            Cx = (float)centerX,
            Cy = (float)centerY,
            Scale = (float)scale,
            Aspect = (float)height / Math.Max(1, width),
            InvW = 1f / Math.Max(1, width),
            InvH = 1f / Math.Max(1, height),
            MaxIter = maxIter,
            Aa = Math.Max(1, aa),
            JuliaCx = (float)juliaCx,
            JuliaCy = (float)juliaCy,
            JuliaOn = julia ? 1 : 0,
            S0 = ToStop(stops[0]),
            S1 = ToStop(stops[1]),
            S2 = ToStop(stops[2]),
            S3 = ToStop(stops[3]),
            S4 = ToStop(stops[4]),
        };
    }

    /// <summary>
    /// Pipeline common to all frames (Render, offscreen benchmark, RenderPreviewToBitmap):
    /// constants, shaders, render target, viewport and draw of the fullscreen triangle.
    /// </summary>
    private static void DrawFrame(DxParams pars, ID3D11RenderTargetView rtv, ID3D11PixelShader ps, int width, int height)
    {
        MappedSubresource mapped = _context!.Map(_cbuffer!, 0, MapMode.WriteDiscard);
        mapped.AsSpan<DxParams>(1)[0] = pars;
        _context.Unmap(_cbuffer!, 0);

        _context.OMSetRenderTargets(rtv);
        _context.VSSetShader(_vs);
        _context.PSSetShader(ps);
        _context.PSSetConstantBuffer(0, _cbuffer!);
        _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _context.RSSetViewport(new Viewport(width, height));
        _context.Draw(3, 0);
    }

    public static void Render(double centerX, double centerY, double scale, int width, int height, int maxIter, int aa, Palette palette, double juliaCx = 0, double juliaCy = 0, bool julia = false)
    {
        if (!IsReady) return;
        DrawFrame(BuildParams(centerX, centerY, scale, width, height, maxIter, aa, palette, juliaCx, juliaCy, julia), _rtv!, _ps!, width, height);
        _swapChain!.Present(0, PresentFlags.None);
    }

    /// <summary>
    /// Prepares the offscreen benchmark: render target in the GPU memory at the
    /// dimensions of the elementary samples grid (e.g. 960x540 AA1x =
    /// 960x540, ~2 MB in R8G8B8A8) plus a ring of event queries to detect
    /// the real completion of the frames. No swapchain, no Present.
    /// </summary>
    public static void BeginBenchmarkOffscreen(int width, int height)
    {
        if (!IsReady) throw new InvalidOperationException("DirectX not initialized.");
        EndBenchmarkOffscreen();
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        // Memory estimate: R8G8B8A8 render target (4 bytes/pixel) x2 margin.
        // If the adapter does not have enough RAM the driver goes into TDR/stall without errors:
        // better a clear error right away than an infinite hang in DrainOne.
        ulong need = (ulong)width * (ulong)height * 4ul * 2ul;
        ulong have = AdapterDedicatedBytes >= ulong.MaxValue - AdapterSharedBytes
            ? ulong.MaxValue : AdapterDedicatedBytes + AdapterSharedBytes;
        if (have != ulong.MaxValue && need > have)
        {
            string msg = "Insufficient GPU memory for the benchmark (" + width + "x" + height + " = ~" + (need / 1048576) + " MB required, ~" + (have / 1048576) + " MB on " + AdapterName + "): reduce AA or use another card.";
            throw new InvalidOperationException(msg);
        }

        var desc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DxgiFormat.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None,
        };
        _benchTarget = _device!.CreateTexture2D(desc);
        _benchRtv = _device.CreateRenderTargetView(_benchTarget);
        _benchQueries = new ID3D11Query?[BenchmarkFlight];
        for (int i = 0; i < _benchQueries.Length; i++)
            _benchQueries[i] = _device.CreateQuery(new QueryDescription(QueryType.Event, QueryFlags.None));
        _benchGridW = width;
        _benchGridH = height;
    }

    /// <summary>Releases the offscreen benchmark resources.</summary>
    public static void EndBenchmarkOffscreen()
    {
        if (_benchQueries != null)
        {
            foreach (var q in _benchQueries)
                q?.Dispose();
            _benchQueries = Array.Empty<ID3D11Query?>();
        }
        _benchRtv?.Dispose(); _benchRtv = null;
        _benchTarget?.Dispose(); _benchTarget = null;
        // Restore the swapchain render target and viewport for subsequent renders.
        if (_rtv != null && _context != null)
        {
            _context.OMSetRenderTargets(_rtv);
            _context.RSSetViewport(new Viewport(_backWidth, _backHeight));
        }
    }

    /// <summary>Frames in flight: how many Draws remain queued before waiting
    /// for the oldest to complete (full pipeline, real throughput).</summary>
    private const int BenchmarkFlight = 4;

    /// <summary>
    /// Offscreen benchmark frame: draws with the iterations-only shader on the
    /// samples grid and queues a completion event. No Present:
    /// the measure is pure shader compute time, independent of the monitor.
    /// </summary>
    private static void RenderBenchmarkOffscreen(double centerX, double centerY, double scale, int width, int height, int maxIter, ID3D11Query query)
    {
        var pars = new DxParams
        {
            Cx = (float)centerX,
            Cy = (float)centerY,
            Scale = (float)scale,
            Aspect = (float)height / Math.Max(1, width),
            InvW = 1f / Math.Max(1, width),
            InvH = 1f / Math.Max(1, height),
            MaxIter = maxIter,
            Aa = 1,
        };
        DrawFrame(pars, _benchRtv!, _benchPs!, width, height);
        _context!.End(query);
    }

    /// <summary>
    /// Standard offscreen measurement loop: renders iterations-only frames on the
    /// samples grid for the given budget and returns the effective seconds and the number
    /// of frames really completed by the GPU (event query). Shared by the benchmark
    /// GUI and by `--bench-dx`. No Present: DWM and cross-GPU copy excluded.
    /// </summary>
    /// <param name="tick">Optional periodic callback (completed frames, seconds).</param>
    public static (double Seconds, int Frames) RunBenchmarkFramesOffscreen(double centerX, double centerY, double scale, int gridW, int gridH, int maxIter, TimeSpan budget, Action<int, double>? tick, CancellationToken ct)
    {
        if (_benchRtv == null || _benchQueries.Length == 0)
            throw new InvalidOperationException("Offscreen benchmark not prepared (BeginBenchmarkOffscreen).");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int submitted = 0, completed = 0;
        double completedSeconds = 0;
        double lastTick = 0;
        var pending = new Queue<(ID3D11Query Query, int Seq)>();

        while (sw.Elapsed < budget)
        {
            ct.ThrowIfCancellationRequested();
            while (pending.Count >= BenchmarkFlight)
                DrainOne(pending, sw, ref completed, ref completedSeconds, ct);
            var query = _benchQueries[submitted % _benchQueries.Length]!;
            RenderBenchmarkOffscreen(centerX, centerY, scale, gridW, gridH, maxIter, query);
            pending.Enqueue((query, submitted));
            submitted++;
            DrainReady(pending, sw, ref completed, ref completedSeconds);
            if (sw.Elapsed.TotalSeconds - lastTick >= 0.5)
            {
                tick?.Invoke(completed, sw.Elapsed.TotalSeconds);
                lastTick = sw.Elapsed.TotalSeconds;
            }
        }

        // Drain the queue: the queued but not yet completed frames count,
        // the time stops at the completion of the last one.
        _context!.Flush();
        while (pending.Count > 0)
            DrainOne(pending, sw, ref completed, ref completedSeconds, ct);
        tick?.Invoke(completed, completedSeconds);
        return (completedSeconds > 0 ? completedSeconds : sw.Elapsed.TotalSeconds, completed);
    }

    /// <summary>
    /// True if the event query fired (the GPU passed the matching `End`).
    /// With pData NULL, `GetData` only does the status check: S_OK = ready, S_FALSE
    /// = still queued; with `DoNotFlush` it does not send queued work to the GPU.
    /// </summary>
    private static bool QuerySignaled(ID3D11Query query, AsyncGetDataFlags flags)
    {
        try
        {
            return _context!.GetData(query, IntPtr.Zero, 0, flags) == SharpGen.Runtime.Result.Ok;
        }
        catch (SharpGen.Runtime.SharpGenException)
        {
            // On device removed GetData throws instead of returning S_FALSE:
            // map it to a readable error with the reason of the removal.
            var removed = _device!.DeviceRemovedReason;
            if (removed.Failure)
                throw new InvalidOperationException(
                    $"GPU hung during the benchmark (device removed, HRESULT 0x{removed.Code:X8}): frame too heavy for {AdapterName} at this grid.");
            throw;
        }
    }

    /// <summary>Counts the frames whose event query already fired (without flush).</summary>
    private static void DrainReady(Queue<(ID3D11Query Query, int Seq)> pending,
        System.Diagnostics.Stopwatch sw, ref int completed, ref double completedSeconds)
    {
        while (pending.Count > 0 && QuerySignaled(pending.Peek().Query, AsyncGetDataFlags.DoNotFlush))
        {
            pending.Dequeue();
            completed++;
            completedSeconds = sw.Elapsed.TotalSeconds;
        }
    }

    /// <summary>Maximum wait of a single frame before declaring the GPU hung.</summary>
    private static readonly TimeSpan BenchmarkFrameTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Waits for the oldest frame in the queue (blocking, interruptible).</summary>
    private static void DrainOne(Queue<(ID3D11Query Query, int Seq)> pending,
        System.Diagnostics.Stopwatch sw, ref int completed, ref double completedSeconds, CancellationToken ct)
    {
        var (query, _) = pending.Dequeue();
        // Poll with flush: the GPU advances while the CPU waits (blocking
        // native GetData would not accept the CancellationToken). With timeout and
        // device-removed check: without it, a card that cannot hold the benchmark grid
        // (Windows TDR, OOM) stays waiting forever without errors.
        var waitStart = sw.Elapsed;
        // Tight poll without sleep: at AA1x the frames last ~1 ms and every
        // wait interval (~1-15 ms) would depress the throughput; the expensive checks
        // (cancel, device-removed, timeout) run every 1024 polls.
        int spins = 0;
        while (!QuerySignaled(query, AsyncGetDataFlags.None))
        {
            if ((++spins & 1023) != 0) continue;
            ct.ThrowIfCancellationRequested();
            var removed = _device!.DeviceRemovedReason;
            if (removed.Failure)
            {
                string msg = $"GPU hung during the benchmark (device removed, HRESULT 0x{removed.Code:X8}): frame too heavy for {AdapterName} at this grid.";
                throw new InvalidOperationException(msg);
            }
            if (sw.Elapsed - waitStart > BenchmarkFrameTimeout)
            {
                string msg = $"GPU timeout ({BenchmarkFrameTimeout.TotalSeconds:0} s) waiting for a frame on {AdapterName}: card too slow or driver hung.";
                throw new TimeoutException(msg);
            }

        }
        completed++;
        completedSeconds = sw.Elapsed.TotalSeconds;
    }

    /// <summary>Compressed name for the UI and the history: "NVIDIA GeForce RTX 5070 Ti"
    /// → "RTX 5070 Ti", "AMD Radeon(TM) Graphics" → "AMD Radeon Graphics".</summary>
    public static string ShortAdapterName(string fullName) =>
        fullName.Replace("NVIDIA GeForce ", "").Replace("(TM)", "").Trim();

    private static DxStop ToStop((double T, byte R, byte G, byte B) s) =>
        new() { R = s.R / 255f, G = s.G / 255f, B = s.B / 255f };

    /// <summary>Captures the backbuffer in a Bitmap (for Save PNG).</summary>
    public static Bitmap Capture()
    {
        if (!IsReady) throw new InvalidOperationException("DirectX not initialized.");
        using ID3D11Texture2D backbuffer = _swapChain!.GetBuffer<ID3D11Texture2D>(0);
        return ReadTextureToBitmap(backbuffer, _backWidth, _backHeight);
    }

    /// <summary>
    /// Renders a COLORED frame of the area onto a render-target outside the
    /// swapchain and returns it as a Bitmap: the benchmark preview for the
    /// DirectX engine (without presenting anything on the main window).
    /// </summary>
    public static Bitmap? RenderPreviewToBitmap(double centerX, double centerY, double scale, int width, int height, int maxIter, int aa, Palette palette, double juliaCx = 0, double juliaCy = 0, bool julia = false)
    {
        if (!IsReady || _context is null) return null;
        try
        {
            var desc = new Texture2DDescription
            {
                Width = (uint)Math.Max(1, width),
                Height = (uint)Math.Max(1, height),
                MipLevels = 1,
                ArraySize = 1,
                // B8G8R8A8: same byte layout of Bitmap Format32bppArgb (B,G,R,A),
                // so the channels do not come out inverted on read.
                Format = DxgiFormat.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None,
            };
            using ID3D11Texture2D target = _device!.CreateTexture2D(desc);
            using ID3D11RenderTargetView rtv = _device.CreateRenderTargetView(target);

            DrawFrame(BuildParams(centerX, centerY, scale, width, height, maxIter, aa, palette, juliaCx, juliaCy, julia), rtv, _ps!, width, height);

            var bmp = ReadTextureToBitmap(target, width, height);

            // Restore the swapchain render target and viewport for subsequent renders
            // (if there is one: in headless it does not exist).
            if (_rtv != null)
            {
                _context.OMSetRenderTargets(_rtv);
                _context.RSSetViewport(new Viewport(_backWidth, _backHeight));
            }
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Copies the content of a texture in a Bitmap (for capture/preview).</summary>
    private static Bitmap ReadTextureToBitmap(ID3D11Texture2D source, int width, int height)
    {
        Texture2DDescription stg = source.Description;
        stg.Usage = ResourceUsage.Staging;
        stg.BindFlags = BindFlags.None;
        stg.CPUAccessFlags = CpuAccessFlags.Read;
        stg.MiscFlags = ResourceOptionFlags.None;
        using ID3D11Texture2D staging = _device!.CreateTexture2D(stg);
        _context!.CopyResource(staging, source); // CopyResource(dst, src): copies the rendered texture to the staging

        MappedSubresource mapped = _context.Map(staging, 0, MapMode.Read);
        try
        {
            var bmp = new Bitmap(
                width, height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var rect = new System.Drawing.Rectangle(0, 0, width, height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
            try
            {
                int rowBytes = width * 4;
                int srcPitch = (int)mapped.RowPitch;
                var row = new byte[rowBytes];
                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(mapped.DataPointer + y * srcPitch, row, 0, rowBytes);
                    Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, rowBytes);
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return bmp;
        }
        finally
        {
            _context.Unmap(staging, 0);
        }
    }

    public static void Dispose()
    {
        EndBenchmarkOffscreen();
        _cbuffer?.Dispose(); _cbuffer = null;
        _benchPs?.Dispose(); _benchPs = null;
        _ps?.Dispose(); _ps = null;
        _vs?.Dispose(); _vs = null;
        _rtv?.Dispose(); _rtv = null;
        _swapChain?.Dispose(); _swapChain = null;
        _context?.Dispose(); _context = null;
        _device?.Dispose(); _device = null;
    }
}
