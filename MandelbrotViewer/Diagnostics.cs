using SharpGen.Runtime;
using Vortice.DXGI;

namespace MandelbrotViewer;

/// <summary>
/// Diagnostica da riga di comando senza UI (MandelbrotViewer --diag-dx | --diag-gpu):
/// verifica l'inizializzazione dei motori DirectX e CUDA. Utile per il troubleshooting
/// quando la finestra principale non parte o un motore non si inizializza.
/// </summary>
internal static class Diagnostics
{
    public static void DiagDx(string? adapterName = null)
    {
        Console.WriteLine("Schede DXGI: " + string.Join(", ", DxMandelbrot.AdapterNames())
            + (DxMandelbrot.EnumerationError.Length > 0 ? "   [ERRORE ENUM: " + DxMandelbrot.EnumerationError + "]" : ""));

        // Enumerazione dettagliata: mostra il risultato di ogni adapter e ogni eventuale
        // eccezione (AdapterNames restituisce solo i nomi validi e nasconde gli errori).
        try
        {
            using IDXGIFactory1 factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            Console.WriteLine("Factory DXGI1: OK");
            for (uint i = 0; i < 32; i++)
            {
                Result enumRes = factory.EnumAdapters(i, out IDXGIAdapter adapter);
                if (enumRes.Failure)
                {
                    Console.WriteLine($"  EnumAdapters({i}): fine ({enumRes.Code})");
                    break;
                }
                using (adapter)
                {
                    try
                    {
                        var desc = adapter.Description;
                        Console.WriteLine($"  Adapter {i}: {desc.Description} | VRAM {desc.DedicatedVideoMemory / (1024 * 1024)} MB");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Adapter {i}: lettura descrizione FALLITA: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Factory/enum DXGI falliti: {ex.GetType().Name}: {ex.Message}");
        }

        using var f = new Form { ShowInTaskbar = false, WindowState = FormWindowState.Minimized, Opacity = 0 };
        f.CreateControl();
        var handle = f.Handle; // forza creazione handle nativo
        bool ok = DxMandelbrot.TryInitialize(handle, 800, 600, adapterName);
        Console.WriteLine("IsReady: " + ok);
        Console.WriteLine("AdapterName: " + DxMandelbrot.AdapterName);
        Console.WriteLine("LastError: " + DxMandelbrot.LastError);
        if (ok)
        {
            try
            {
                DxMandelbrot.Render(MandelbrotForm.StartCenterX, MandelbrotForm.StartCenterY, MandelbrotForm.StartScale, 800, 600, 200, 1, Palette.Fuoco);
                Console.WriteLine("Render: OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Render fallito: " + ex);
            }
        }
        DxMandelbrot.Dispose();
    }

    public static void DiagGpu()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "diag_gpu.txt");
        try
        {
            var devices = GpuMandelbrot.DeviceNames();
            bool ok = GpuMandelbrot.TryInitialize();
            using var bmp = new Bitmap(320, 240);
            if (ok)
                GpuMandelbrot.Render(bmp, MandelbrotForm.StartCenterX, MandelbrotForm.StartCenterY, MandelbrotForm.StartScale, 200, Palette.Fuoco, 1, false, CancellationToken.None);
            File.WriteAllText(path, $"Devices: {string.Join(", ", devices)}\nReady: {ok}\nDevice: {GpuMandelbrot.DeviceName}\nError: {GpuMandelbrot.LastError}");
        }
        catch (Exception ex)
        {
            File.WriteAllText(path, $"Exception: {ex}");
        }
        finally
        {
            GpuMandelbrot.Dispose();
        }
    }

    /// <summary>
    /// Triplo test standard su ogni scheda DirectX disponibile (o solo su quella
    /// indicata): per scheda esegue <paramref name="runs"/> run del benchmark
    /// standardizzato e stampa i valori in MPixel/s con il migliore — la misura
    /// usata per lo storico del grafico benchmark (nome compresso).
    /// Offscreen senza finestra né Present (headless): solo shader + event query,
    /// così DWM e copia inter-GPU non falsano le schede senza monitor.
    /// </summary>
    public static void BenchDx(string? adapterName, int runs, TimeSpan budget, string? csvPath = null)
    {
        int gridW = BenchmarkStandard.Width * BenchmarkStandard.Aa;
        int gridH = BenchmarkStandard.Height * BenchmarkStandard.Aa;
        Console.WriteLine($"Benchmark DirectX standardizzato (offscreen, senza Present): {runs} run da {budget.TotalSeconds:0} s per scheda, " +
            $"zona {BenchmarkStandard.Width}x{BenchmarkStandard.Height} AA{BenchmarkStandard.Aa} " +
            $"(griglia {gridW}x{gridH}, {BenchmarkStandard.PixelsPerFrame / 1e6:0.##} MPixel/frame), " +
            $"{BenchmarkStandard.MaxIter} iter, solo iterazioni, completamento via event query.");

        IReadOnlyList<string> schede = adapterName != null
            ? new[] { adapterName }
            : DxMandelbrot.AdapterNames();
        if (schede.Count == 0)
        {
            Console.WriteLine("Nessuna scheda DirectX disponibile. " +
                (DxMandelbrot.EnumerationError.Length > 0 ? "(" + DxMandelbrot.EnumerationError + ")" : ""));
            return;
        }

        var migliori = new List<(string Short, double Best)>();
        foreach (string scheda in schede)
        {
            string shortName = DxMandelbrot.ShortAdapterName(scheda);
            Console.WriteLine();
            Console.WriteLine($"=== {shortName} ===");
            if (!DxMandelbrot.TryInitializeHeadless(scheda))
            {
                Console.WriteLine("  init fallita: " + DxMandelbrot.LastError);
                continue;
            }

            double best = 0;
            try
            {
                try
                {
                    DxMandelbrot.BeginBenchmarkOffscreen(gridW, gridH);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  griglia offscreen non creata: " + ex.Message);
                    continue;
                }
                for (int r = 1; r <= runs; r++)
                {
                    try
                    {
                        var (seconds, frames) = DxMandelbrot.RunBenchmarkFramesOffscreen(
                            BenchmarkStandard.CenterX, BenchmarkStandard.CenterY, BenchmarkStandard.Scale,
                            gridW, gridH, BenchmarkStandard.MaxIter, budget, null, CancellationToken.None);
                        double mps = BenchmarkStandard.PixelsPerSecond(frames, seconds) / 1e6;
                        best = Math.Max(best, mps);
                        Console.WriteLine($"  run {r}: {frames} frame in {seconds:0.00} s  →  {mps:0.#} MPixel/s");
                        WriteCsvRow(csvPath, "DirectX", shortName, "float", r, frames, seconds, mps);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  run {r} fallito: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
            finally
            {
                DxMandelbrot.EndBenchmarkOffscreen();
                DxMandelbrot.Dispose();
            }

            if (best > 0)
            {
                migliori.Add((shortName, best));
                Console.WriteLine($"  MIGLIORE: {best:0.#} MPixel/s");
            }
            else
            {
                Console.WriteLine("  nessuna misura valida");
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== Riepilogo (best per scheda, MPixel/s) ===");
        foreach (var (shortName, best) in migliori)
            Console.WriteLine($"  {shortName}: {best:0.#}");
    }

    /// <summary>
    /// Triplo test standard su ogni device CUDA disponibile (o solo su quello
    /// indicato): per device esegue <paramref name="runs"/> run del benchmark
    /// standardizzato in float 32-bit e in double 64-bit e stampa i valori in
    /// MPixel/s con il migliore — la misura usata per lo storico del grafico
    /// benchmark. Analogo di <see cref="BenchDx"/> per il motore CUDA.
    /// </summary>
    public static void BenchCuda(string? deviceName, int runs, TimeSpan budget, string? csvPath = null)
    {
        Console.WriteLine($"Benchmark CUDA standardizzato: {runs} run da {budget.TotalSeconds:0} s per device, " +
            $"zona {BenchmarkStandard.Width}x{BenchmarkStandard.Height} AA{BenchmarkStandard.Aa} " +
            $"({BenchmarkStandard.PixelsPerFrame / 1e6:0.##} MPixel/frame), " +
            $"{BenchmarkStandard.MaxIter} iter, solo iterazioni (float 32-bit + double 64-bit).");

        IReadOnlyList<string> devices = deviceName != null
            ? new[] { deviceName }
            : GpuMandelbrot.DeviceNames();
        if (devices.Count == 0)
        {
            Console.WriteLine("Nessun device CUDA disponibile. (" + GpuMandelbrot.LastError + ")");
            return;
        }

        var migliori = new List<(string Short, double Best32, double Best64)>();
        foreach (string device in devices)
        {
            string shortName = device.Replace("NVIDIA GeForce ", "").Trim();
            Console.WriteLine();
            Console.WriteLine($"=== {shortName} ===");
            if (!GpuMandelbrot.TryInitialize(device))
            {
                Console.WriteLine("  init fallita: " + GpuMandelbrot.LastError);
                continue;
            }

            double best32 = 0, best64 = 0;
            foreach (bool useDouble in new[] { false, true })
            {
                string tag = useDouble ? "64-bit" : "32-bit";
                for (int r = 1; r <= runs; r++)
                {
                    try
                    {
                        var (_, seconds, frames) = GpuMandelbrot.BenchmarkGpu(
                            BenchmarkStandard.CenterX, BenchmarkStandard.CenterY, BenchmarkStandard.Scale,
                            BenchmarkStandard.Width, BenchmarkStandard.Height, BenchmarkStandard.MaxIter,
                            BenchmarkStandard.Aa, useDouble, budget, null, CancellationToken.None);
                        double mps = BenchmarkStandard.PixelsPerSecond(frames, seconds) / 1e6;
                        if (useDouble) best64 = Math.Max(best64, mps);
                        else best32 = Math.Max(best32, mps);
                        Console.WriteLine($"  {tag} run {r}: {frames} frame in {seconds:0.00} s  →  {mps:0.#} MPixel/s");
                        WriteCsvRow(csvPath, "CUDA", shortName, tag, r, frames, seconds, mps);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  {tag} run {r} fallito: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
            GpuMandelbrot.Dispose();

            if (best32 > 0 || best64 > 0)
            {
                migliori.Add((shortName, best32, best64));
                Console.WriteLine($"  MIGLIORE 32-bit: {best32:0.#} MPixel/s");
                Console.WriteLine($"  MIGLIORE 64-bit: {best64:0.#} MPixel/s");
            }
            else
            {
                Console.WriteLine("  nessuna misura valida");
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== Riepilogo (best per device, MPixel/s) ===");
        foreach (var (shortName, best32, best64) in migliori)
            Console.WriteLine($"  {shortName}: 32-bit {best32:0.#} | 64-bit {best64:0.#}");
    }

    /// <summary>Nome modello della CPU (da registro, senza suffissi), es.
    /// "AMD Ryzen 9 9900X" o "Intel Core i7-14700K".</summary>
    public static string CpuName()
    {
        try
        {
            string full = (Microsoft.Win32.Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                "ProcessorNameString", "") as string ?? "").Trim();
            if (full.Length == 0) return "CPU";
            int at = full.IndexOf(" @ ", StringComparison.Ordinal);
            if (at > 0) full = full[..at];
            return full.Replace("(R)", "").Replace("(TM)", "")
                .Replace(" Processor", "").Replace(" 12-Core", "").Replace(" 16-Core", "")
                .Replace(" 8-Core", "").Replace(" 6-Core", "").Replace(" 24-Core", "")
                .Replace(" 32-Core", "").Replace("  ", " ").Trim();
        }
        catch
        {
            return "CPU";
        }
    }

    /// <summary>
    /// Triplo test standard sulla CPU: esegue <paramref name="runs"/> run del
    /// benchmark standardizzato (`Mandelbrot.BenchmarkCpu`, double) e stampa i
    /// valori in MPixel/s con il migliore — la misura usata per lo storico CPU
    /// del grafico benchmark (con nome modello).
    /// </summary>
    public static void BenchCpu(int runs, TimeSpan budget, string? csvPath = null)
    {
        Console.WriteLine($"Benchmark CPU standardizzato ({CpuName()}): {runs} run da {budget.TotalSeconds:0} s, " +
            $"zona {BenchmarkStandard.Width}x{BenchmarkStandard.Height} AA{BenchmarkStandard.Aa} " +
            $"({BenchmarkStandard.PixelsPerFrame / 1e6:0.##} MPixel/frame), " +
            $"{BenchmarkStandard.MaxIter} iter, double, solo iterazioni.");

        double best = 0;
        for (int r = 1; r <= runs; r++)
        {
            try
            {
                var (_, seconds, frames) = Mandelbrot.BenchmarkCpu(
                    BenchmarkStandard.CenterX, BenchmarkStandard.CenterY, BenchmarkStandard.Scale,
                    BenchmarkStandard.Width, BenchmarkStandard.Height, BenchmarkStandard.MaxIter,
                    BenchmarkStandard.Aa, budget, null, CancellationToken.None);
                double mps = BenchmarkStandard.PixelsPerSecond(frames, seconds) / 1e6;
                best = Math.Max(best, mps);
                Console.WriteLine($"  run {r}: {frames} frame in {seconds:0.00} s  →  {mps:0.#} MPixel/s");
                WriteCsvRow(csvPath, "CPU", CpuName(), "double", r, frames, seconds, mps);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  run {r} fallito: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"=== Riepilogo (best, MPixel/s): {CpuName()}: {best:0.#} ===");
    }

    /// <summary>Scrive una riga CSV se richiesto (errori non fatali: avviso e via).</summary>
    private static void WriteCsvRow(string? csvPath, string engine, string device, string precision,
        int run, int frames, double seconds, double mps)
    {
        if (csvPath == null) return;
        try
        {
            BenchmarkCsv.AppendRow(csvPath, engine, device, precision, run, frames, seconds, mps);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  CSV non scritto ({csvPath}): {ex.Message}");
        }
    }
}
