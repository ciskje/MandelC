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
}
