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
            float2 c = float2(cx + (p.x * invW - 0.5) * scale,
                              cy + (p.y * invH - 0.5) * scale * aspect);
            float2 z = 0.0;
            int iter = 0;
            while (iter < maxIter && dot(z, z) <= 4.0)
            {
                z = float2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + c;
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

    // Stato del benchmark: la swapchain viene ingrandita alla griglia di campioni
    // (es. 7680x4320) e ripristinata alla fine; nel frattempo i resize del pannello
    // vengono ricordati ma non applicati.
    private static bool _benchmarking;
    private static int _preBenchWidth, _preBenchHeight;

    public static bool IsReady => _device != null;
    public static string LastError { get; private set; } = "";
    /// <summary>Nome della scheda video attualmente usata ("" se non inizializzato).</summary>
    public static string AdapterName { get; private set; } = "";

    /// <summary>Schede video hardware disponibili (esclusi i renderizzatori software WARP).</summary>
    public static IReadOnlyList<string> AdapterNames()
    {
        var names = new List<string>();
        try
        {
            using IDXGIFactory1 factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            for (uint i = 0; i < 32; i++)
            {
                if (factory.EnumAdapters(i, out IDXGIAdapter adapter).Failure) break;
                using (adapter)
                {
                    if (adapter.Description.DedicatedVideoMemory > 0)
                        names.Add(adapter.Description.Description);
                }
            }
        }
        catch
        {
            // Enumerazione non riuscita: nessuna scheda riportata.
        }
        return names;
    }

    /// <param name="adapterName">Scheda da usare (nome DXGI esatto); null = adapter hardware predefinito.
    /// Con una scheda richiesta, il device viene creato esplicitamente sull'adapter scelto.</param>
    public static bool TryInitialize(IntPtr hwnd, int width, int height, string? adapterName = null)
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
            _swapChain = factory.CreateSwapChainForHwnd(_device, hwnd, desc, fullscreen);

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
        if (_benchmarking)
        {
            // Durante il benchmark la misura usa la griglia di campioni fissa:
            // ignora i resize del pannello ma ricorda le nuove dimensioni da ripristinare.
            _preBenchWidth = width;
            _preBenchHeight = height;
            return;
        }
        if (width == _backWidth && height == _backHeight) return;
        _rtv?.Dispose();
        _rtv = null;
        _swapChain!.ResizeBuffers(0, (uint)width, (uint)height, DxgiFormat.Unknown, SwapChainFlags.None);
        CreateViews(width, height);
    }

    /// <summary>Costruisce i parametri di un frame colorato (con gli stop della palette).</summary>
    private static DxParams BuildParams(double centerX, double centerY, double scale, int width, int height, int maxIter, int aa, Palette palette)
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
            S0 = ToStop(stops[0]),
            S1 = ToStop(stops[1]),
            S2 = ToStop(stops[2]),
            S3 = ToStop(stops[3]),
            S4 = ToStop(stops[4]),
        };
    }

    /// <summary>
    /// Pipeline comune a tutti i frame (Render, RenderBenchmark, RenderPreviewToBitmap):
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

    public static void Render(double centerX, double centerY, double scale, int width, int height, int maxIter, int aa, Palette palette)
    {
        if (!IsReady) return;
        DrawFrame(BuildParams(centerX, centerY, scale, width, height, maxIter, aa, palette), _rtv!, _ps!, width, height);
        _swapChain!.Present(0, PresentFlags.None);
    }

    /// <summary>
    /// Prepara il benchmark: ingrandisce la swapchain alla griglia dei campioni
    /// elementari (es. 960x540 AA8 = 7680x4320), senza media dei campioni.
    /// </summary>
    /// <param name="width">Larghezza della griglia di campioni.</param>
    /// <param name="height">Altezza della griglia di campioni.</param>
    public static void BeginBenchmark(int width, int height)
    {
        if (!IsReady || _benchmarking) return;
        _preBenchWidth = _backWidth;
        _preBenchHeight = _backHeight;
        Resize(width, height); // prima del flag: con _benchmarking=true Resize ignorerebbe la richiesta
        _benchmarking = true;
    }

    /// <summary>Ripristina la risoluzione della swapchain pre-benchmark.</summary>
    public static void EndBenchmark()
    {
        if (!IsReady || !_benchmarking) return;
        _benchmarking = false;
        Resize(_preBenchWidth, _preBenchHeight);
    }

    /// <summary>
    /// Frame di benchmark: disegna con lo shader solo-iterazioni sulla griglia
    /// dei campioni e presenta SENZA v-sync (SyncInterval 0), così la misura
    /// riflette il reale tempo di calcolo della GPU, non il refresh del monitor.
    /// </summary>
    public static void RenderBenchmark(double centerX, double centerY, double scale, int width, int height, int maxIter)
    {
        if (!IsReady) return;
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
        DrawFrame(pars, _rtv!, _benchPs!, width, height);
        _swapChain!.Present(0, PresentFlags.None);
    }

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
    public static Bitmap? RenderPreviewToBitmap(double centerX, double centerY, double scale, int width, int height, int maxIter, int aa, Palette palette)
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

            DrawFrame(BuildParams(centerX, centerY, scale, width, height, maxIter, aa, palette), rtv, _ps!, width, height);

            var bmp = ReadTextureToBitmap(target, width, height);

            // Ripristina render target e viewport della swapchain per i render successivi.
            _context.OMSetRenderTargets(_rtv!);
            _context.RSSetViewport(new Viewport(_backWidth, _backHeight));
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
