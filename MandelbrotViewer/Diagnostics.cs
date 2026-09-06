namespace MandelbrotViewer;

/// <summary>
/// Diagnostica da riga di comando senza UI (MandelbrotViewer --diag-dx | --diag-gpu):
/// verifica l'inizializzazione dei motori DirectX e CUDA. Utile per il troubleshooting
/// quando la finestra principale non parte o un motore non si inizializza.
/// </summary>
internal static class Diagnostics
{
    public static void DiagDx()
    {
        Console.WriteLine("Schede DXGI: " + string.Join(", ", DxMandelbrot.AdapterNames()));
        using var f = new Form { ShowInTaskbar = false, WindowState = FormWindowState.Minimized, Opacity = 0 };
        f.CreateControl();
        var handle = f.Handle; // forza creazione handle nativo
        bool ok = DxMandelbrot.TryInitialize(handle, 800, 600);
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
