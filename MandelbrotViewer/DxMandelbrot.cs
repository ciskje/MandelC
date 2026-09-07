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
/// Backend DirectX 11 realtime: triangolo fullscreen + pixel shader HLSL che calcola
/// il frattale ogni frame (float). Presenta su swapchain legata all'handle del pannello.
/// </summary>
internal static class DxMandelbrot
{
    // Triangolo fullscreen senza vertex buffer (SV_VertexID).
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
            // Julia: z(0) = punto del pixel, c = costante; Mandelbrot: z(0) = 0, c = pixel.
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
                // Mappatura allineata con PaletteColors (CPU) e GpuMandelbrot.ColorFromIterations (CUDA).
                float t = saturate(smoothIterations / (float)maxIter * 1.35 + 0.03);
                col = Graded(t);
            }
            acc += col;
        }
    }
    return float4(acc / (float)(n * n), 1.0);
}";

    /// <summary>
    /// Shader benchmark: soltanto il conteggio iterazioni del frattale, senza
    /// colorazione, senza smooth e senza media dei campioni. Ogni pixel della
    /// griglia è un campione elementare: identico al lavoro dei kernel CUDA/CPU.
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
    // L'uscita dipende dalle iterazioni: il compilatore non può eliminare il loop.
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

    // Benchmark offscreen (v2.5.20): render target in memoria della GPU testata,
    // senza swapchain né Present — niente DWM, niente copia inter-GPU verso la
    // scheda del monitor (che falsava le schede headless, vedi SPECIFICHE).
    // Draw ritorna subito (comandi in coda): il completamento dei frame è rilevato
    // con un anello di event query.
    private static ID3D11Texture2D? _benchTarget;
    private static ID3D11RenderTargetView? _benchRtv;
    private static ID3D11Query?[] _benchQueries = Array.Empty<ID3D11Query?>();
    private static int _benchGridW, _benchGridH;

    public static bool IsReady => _device != null;
    public static string LastError { get; private set; } = "";
    /// <summary>Nome della scheda video attualmente usata ("" se non inizializzato).</summary>
    public static string AdapterName { get; private set; } = "";

    /// <summary>Memoria dedicata + condivisa dell'adapter in uso (ulong.MaxValue se ignota).</summary>
    public static ulong AdapterDedicatedBytes { get; private set; } = ulong.MaxValue;
    public static ulong AdapterSharedBytes { get; private set; } = ulong.MaxValue;

    /// <summary>Errore dell'ultima enumerazione fallita (diagnostica), vuoto se OK.</summary>
    public static string EnumerationError { get; private set; } = "";

    /// <summary>Schede video hardware disponibili (esclusi i renderizzatori software WARP).</summary>
    /// <remarks>
    /// NON leggere né confrontare direttamente DedicatedVideoMemory: è un
    /// PointerUSize (SIZE_T) e la conversione implicita di SharpGen passa per 32 bit
    /// (UIntPtr.ToUInt32), che lancia OverflowException con GPU da più di 4 GB — è il
    /// motivo per cui l'enumerazione restava vuota su macchine con GPU moderne
    /// (cfr. nota v2.3.8 "overflow del wrapper"). I renderizzatori software si
    /// escludono per nome: "Microsoft Basic Render Driver" è il WARP di D3D11.
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

    /// <summary>Legge la memoria dell'adapter scelto (o del primo hardware se auto).
    /// Le dimensioni DXGI oltre i 4 GB mandano in overflow la conversione a 32 bit
    /// del wrapper: in quel caso (o se l'adapter non si trova) resta ulong.MaxValue
    /// = memoria abbondante/sconosciuta e il controllo VRAM viene saltato.</summary>
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
        catch { /* memoria sconosciuta: nessun controllo */ }
    }

    private static ulong SafeMemBytes(Func<ulong> read)
    {
        try { return read(); }
        catch { return ulong.MaxValue; }
    }

    /// <param name="adapterName">Scheda da usare (nome DXGI esatto); null = adapter hardware predefinito.
    /// Con una scheda richiesta, il device viene creato esplicitamente sull'adapter scelto.</param>
    public static bool TryInitialize(IntPtr hwnd, int width, int height, string? adapterName = null)
    {
        if (!EnsureDevice(adapterName))
            return false;
        LastError = "";
        string step = "inizio";
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
    /// Inizializza device, context e shader sulla scheda scelta senza finestra né
    /// swapchain (headless): per il benchmark offscreen, che non presenta nulla.
    /// </summary>
    /// <param name="adapterName">Scheda da usare (nome DXGI esatto); null = predefinito.</param>
    public static bool TryInitializeHeadless(string? adapterName = null) =>
        EnsureDevice(adapterName);

    /// <summary>
    /// Crea (o riusa) device, context e shader sull'adapter scelto. La swapchain
    /// resta di competenza di <see cref="TryInitialize"/> (serve una finestra).
    /// </summary>
    private static bool EnsureDevice(string? adapterName)
    {
        bool same = adapterName == null ||
            string.Equals(AdapterName, adapterName, StringComparison.OrdinalIgnoreCase);
        if (IsReady)
        {
            if (same) return true;
            Dispose(); // cambio scheda: il device va ricreato sull'adapter scelto
        }
        LastError = "";
        string step = "inizio";
        try
        {
            step = "DXGI.CreateDXGIFactory2";
            using IDXGIFactory2 factory = DXGI.CreateDXGIFactory2<IDXGIFactory2>(false);

            // Se l'utente ha scelto una scheda, trova l'adapter DXGI corrispondente
            // e passalo esplicito a D3D11CreateDevice (con DriverType.Unknown, come
            // richiesto quando si passa un adapter); altrimenti adapter predefinito.
            IDXGIAdapter? chosen = null;
            if (adapterName != null)
            {
                step = "ricerca dell'adapter DXGI richiesto";
                for (uint i = 0; i < 32; i++)
                {
                    if (factory.EnumAdapters(i, out IDXGIAdapter adapter).Failure) break;
                    if (string.Equals(adapter.Description.Description, adapterName, StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = adapter; // il dispose avviene dopo la creazione del device
                        break;
                    }
                    adapter.Dispose();
                }
                if (chosen == null)
                    throw new InvalidOperationException($"Scheda video non trovata: {adapterName}");
            }

            step = chosen != null
                ? $"D3D11CreateDevice (adapter scelto: {adapterName})"
                : "D3D11CreateDevice (adapter hardware predefinito)";
            var result = D3D11.D3D11CreateDevice(
                chosen, chosen != null ? DriverType.Unknown : DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_0 },
                out ID3D11Device device,
                out FeatureLevel level,
                out ID3D11DeviceContext context);
            chosen?.Dispose(); // il device mantiene il proprio riferimento all'adapter
            if (result.Failure)
                throw new InvalidOperationException($"D3D11CreateDevice fallito (HRESULT 0x{result.Code:X8})");
            _device = device;
            _context = context;
            AdapterName = adapterName ?? "Auto (adapter hardware predefinito)";
            RefreshAdapterMemory(adapterName);

            step = "D3DCompiler.Compile (shader)";
            ReadOnlyMemory<byte> vsCode = Compiler.Compile(VsSource, "VS", "mandelbrot-vs", "vs_5_0");
            ReadOnlyMemory<byte> psCode = Compiler.Compile(PsSource, "PS", "mandelbrot-ps", "ps_5_0");
            _vs = _device.CreateVertexShader(vsCode.Span);
            _ps = _device.CreatePixelShader(psCode.Span);

            step = "D3DCompiler.Compile (shader benchmark)";
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

    /// <summary>Costruisce i parametri di un frame colorato (con gli stop della palette).</summary>
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
    /// Pipeline comune a tutti i frame (Render, benchmark offscreen, RenderPreviewToBitmap):
    /// costanti, shader, render target, viewport e draw del triangolo fullscreen.
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
    /// Prepara il benchmark offscreen: render target in memoria della GPU alle
    /// dimensioni della griglia dei campioni elementari (es. 960x540 AA1x =
    /// 960x540, ~2 MB in R8G8B8A8) più un anello di event query per rilevare
    /// il completamento reale dei frame. Niente swapchain, niente Present.
    /// </summary>
    public static void BeginBenchmarkOffscreen(int width, int height)
    {
        if (!IsReady) throw new InvalidOperationException("DirectX non inizializzato.");
        EndBenchmarkOffscreen();
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        // Stima memoria: render target R8G8B8A8 (4 byte/pixel) x2 di margine.
        // Se l'adapter non ha abbastanza RAM il driver va in TDR/stallo senza errori:
        // meglio un errore chiaro subito che un blocco infinito in DrainOne.
        ulong need = (ulong)width * (ulong)height * 4ul * 2ul;
        ulong have = AdapterDedicatedBytes >= ulong.MaxValue - AdapterSharedBytes
            ? ulong.MaxValue : AdapterDedicatedBytes + AdapterSharedBytes;
        if (have != ulong.MaxValue && need > have)
        {
            string msg = "Memoria GPU insufficiente per il benchmark (" + width + "x" + height + " = ~" + (need / 1048576) + " MB richiesti, ~" + (have / 1048576) + " MB su " + AdapterName + "): ridurre AA o usare un'altra scheda.";
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

    /// <summary>Rilascia le risorse del benchmark offscreen.</summary>
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
        // Ripristina render target e viewport della swapchain per i render successivi.
        if (_rtv != null && _context != null)
        {
            _context.OMSetRenderTargets(_rtv);
            _context.RSSetViewport(new Viewport(_backWidth, _backHeight));
        }
    }

    /// <summary>Frame in flight: quanti Draw restano accodati prima di attendere
    /// il completamento del più vecchio (pipeline piena, throughput reale).</summary>
    private const int BenchmarkFlight = 4;

    /// <summary>
    /// Frame di benchmark offscreen: disegna con lo shader solo-iterazioni sulla
    /// griglia dei campioni e accoda un evento di completamento. Nessun Present:
    /// la misura è puro tempo di calcolo dello shader, indipendente dal monitor.
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
    /// Loop di misura standard offscreen: rende frame solo-iterazioni sulla griglia
    /// dei campioni per il budget indicato e ritorna i secondi effettivi e il numero
    /// di frame davvero completati dalla GPU (event query). Condiviso dal benchmark
    /// GUI e da `--bench-dx`. Senza Present: DWM e copia inter-GPU esclusi.
    /// </summary>
    /// <param name="tick">Callback opzionale (frames completati, secondi) periodica.</param>
    public static (double Seconds, int Frames) RunBenchmarkFramesOffscreen(double centerX, double centerY, double scale, int gridW, int gridH, int maxIter, TimeSpan budget, Action<int, double>? tick, CancellationToken ct)
    {
        if (_benchRtv == null || _benchQueries.Length == 0)
            throw new InvalidOperationException("Benchmark offscreen non preparato (BeginBenchmarkOffscreen).");
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

        // Svuota la coda: i frame accodati ma non ancora completati contano,
        // il tempo si ferma al completamento dell'ultimo.
        _context!.Flush();
        while (pending.Count > 0)
            DrainOne(pending, sw, ref completed, ref completedSeconds, ct);
        tick?.Invoke(completed, completedSeconds);
        return (completedSeconds > 0 ? completedSeconds : sw.Elapsed.TotalSeconds, completed);
    }

    /// <summary>
    /// True se l'event query è scattata (la GPU ha superato l'`End` corrispondente).
    /// Con pData NULL, `GetData` fa solo il check di stato: S_OK = pronta, S_FALSE
    /// = ancora in coda; con `DoNotFlush` non invia lavoro accodato alla GPU.
    /// </summary>
    private static bool QuerySignaled(ID3D11Query query, AsyncGetDataFlags flags)
    {
        try
        {
            return _context!.GetData(query, IntPtr.Zero, 0, flags) == SharpGen.Runtime.Result.Ok;
        }
        catch (SharpGen.Runtime.SharpGenException)
        {
            // Su device removed GetData lancia invece di tornare S_FALSE:
            // mappa in errore leggibile con il motivo della rimozione.
            var removed = _device!.DeviceRemovedReason;
            if (removed.Failure)
                throw new InvalidOperationException(
                    $"GPU bloccata durante il benchmark (device removed, HRESULT 0x{removed.Code:X8}): frame troppo pesante per {AdapterName} a questa griglia.");
            throw;
        }
    }

    /// <summary>Conta i frame la cui event query è già scattata (senza flush).</summary>
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

    /// <summary>Attesa massima di un singolo frame prima di dichiarare la GPU bloccata.</summary>
    private static readonly TimeSpan BenchmarkFrameTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Attende il frame più vecchio della coda (bloccante, interrompibile).</summary>
    private static void DrainOne(Queue<(ID3D11Query Query, int Seq)> pending,
        System.Diagnostics.Stopwatch sw, ref int completed, ref double completedSeconds, CancellationToken ct)
    {
        var (query, _) = pending.Dequeue();
        // Poll con flush: la GPU avanza mentre la CPU attende (GetData bloccante
        // nativo non accetterebbe il CancellationToken). Con timeout e controllo
        // device-removed: senza, una scheda che non regge la griglia del benchmark
        // (TDR di Windows, OOM) resta in attesa per sempre senza errori.
        var waitStart = sw.Elapsed;
        // Poll stretto senza sleep: a AA1x i frame durano ~1 ms e ogni quanto
        // di attesa (~1-15 ms) deprimerebbe il throughput; i controlli costosi
        // (cancel, device-removed, timeout) girano ogni 1024 poll.
        int spins = 0;
        while (!QuerySignaled(query, AsyncGetDataFlags.None))
        {
            if ((++spins & 1023) != 0) continue;
            ct.ThrowIfCancellationRequested();
            var removed = _device!.DeviceRemovedReason;
            if (removed.Failure)
            {
                string msg = $"GPU bloccata durante il benchmark (device removed, HRESULT 0x{removed.Code:X8}): frame troppo pesante per {AdapterName} a questa griglia.";
                throw new InvalidOperationException(msg);
            }
            if (sw.Elapsed - waitStart > BenchmarkFrameTimeout)
            {
                string msg = $"Timeout GPU ({BenchmarkFrameTimeout.TotalSeconds:0} s) in attesa di un frame su {AdapterName}: scheda troppo lenta o driver bloccato.";
                throw new TimeoutException(msg);
            }

        }
        completed++;
        completedSeconds = sw.Elapsed.TotalSeconds;
    }

    /// <summary>Nome compresso per la UI e lo storico: "NVIDIA GeForce RTX 5070 Ti"
    /// → "RTX 5070 Ti", "AMD Radeon(TM) Graphics" → "AMD Radeon Graphics".</summary>
    public static string ShortAdapterName(string fullName) =>
        fullName.Replace("NVIDIA GeForce ", "").Replace("(TM)", "").Trim();

    private static DxStop ToStop((double T, byte R, byte G, byte B) s) =>
        new() { R = s.R / 255f, G = s.G / 255f, B = s.B / 255f };

    /// <summary>Cattura il backbuffer in un Bitmap (per Salva PNG).</summary>
    public static Bitmap Capture()
    {
        if (!IsReady) throw new InvalidOperationException("DirectX non inizializzato.");
        using ID3D11Texture2D backbuffer = _swapChain!.GetBuffer<ID3D11Texture2D>(0);
        return ReadTextureToBitmap(backbuffer, _backWidth, _backHeight);
    }

    /// <summary>
    /// Renderizza un frame COLORATO della zona su una render-target fuori dalla
    /// swapchain e lo restituisce come Bitmap: la preview del benchmark per il
    /// motore DirectX (senza presentare nulla sulla finestra principale).
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
                // B8G8R8A8: stesso layout di byte di Bitmap Format32bppArgb (B,G,R,A),
                // così i canali non risultano invertiti in lettura.
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

            // Ripristina render target e viewport della swapchain per i render successivi
            // (se c'è una swapchain: in headless non esiste).
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

    /// <summary>Copia il contenuto di una texture in un Bitmap (per cattura/anteprima).</summary>
    private static Bitmap ReadTextureToBitmap(ID3D11Texture2D source, int width, int height)
    {
        Texture2DDescription stg = source.Description;
        stg.Usage = ResourceUsage.Staging;
        stg.BindFlags = BindFlags.None;
        stg.CPUAccessFlags = CpuAccessFlags.Read;
        stg.MiscFlags = ResourceOptionFlags.None;
        using ID3D11Texture2D staging = _device!.CreateTexture2D(stg);
        _context!.CopyResource(staging, source); // CopyResource(dst, src): copia la texture renderizzata nello staging

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
