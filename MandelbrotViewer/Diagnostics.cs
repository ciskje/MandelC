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
                DxMandelbrot.Render(-0.5, 0.0, 3.2, 800, 600, 200, 1, Palette.Fuoco);
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
                GpuMandelbrot.Render(bmp, -0.5, 0, 3.2, 200, Palette.Fuoco, 1, false, CancellationToken.None);
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
    /// Usa una piccola finestra VISIBILE: con Present(0) su una finestra nascosta
    /// il compositor potrebbe saltare il lavoro GPU e falsare la misura.
    /// </summary>
    public static void BenchDx(string? adapterName, int runs, TimeSpan budget)
    {
        int gridW = BenchmarkStandard.Width * BenchmarkStandard.Aa;
        int gridH = BenchmarkStandard.Height * BenchmarkStandard.Aa;
        Console.WriteLine($"Benchmark DirectX standardizzato: {runs} run da {budget.TotalSeconds:0} s per scheda, " +
            $"zona {BenchmarkStandard.Width}x{BenchmarkStandard.Height} AA{BenchmarkStandard.Aa} " +
            $"(griglia {gridW}x{gridH}, {BenchmarkStandard.PixelsPerFrame / 1e6:0.##} MPixel/frame), " +
            $"{BenchmarkStandard.MaxIter} iter, solo iterazioni, senza v-sync.");

        using var f = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Bounds = new Rectangle(SystemInformation.VirtualScreen.Width - 280,
                                   SystemInformation.VirtualScreen.Height - 170, 230, 110),
            Text = "MandelC# --bench-dx",
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            TopMost = true,
        };
        f.Show();
        var handle = f.Handle;

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
            if (!DxMandelbrot.TryInitialize(handle, 320, 240, scheda))
            {
                Console.WriteLine("  init fallita: " + DxMandelbrot.LastError);
                continue;
            }

            double best = 0;
            try
            {
                DxMandelbrot.BeginBenchmark(gridW, gridH);
                for (int r = 1; r <= runs; r++)
                {
                    try
                    {
                        var (seconds, frames) = DxMandelbrot.RunBenchmarkFrames(
                            BenchmarkStandard.CenterX, BenchmarkStandard.CenterY, BenchmarkStandard.Scale,
                            gridW, gridH, BenchmarkStandard.MaxIter, budget, null, CancellationToken.None);
                        double mps = BenchmarkStandard.PixelsPerSecond(frames, seconds) / 1e6;
                        best = Math.Max(best, mps);
                        Console.WriteLine($"  run {r}: {frames} frame in {seconds:0.00} s  →  {mps:0.#} MPixel/s");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  run {r} fallito: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
            finally
            {
                DxMandelbrot.EndBenchmark();
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
}
