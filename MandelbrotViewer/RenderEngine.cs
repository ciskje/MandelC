namespace MandelbrotViewer;

/// <summary>
/// Motore di rendering del frattale. Roadmap: `Cuda` (v1.4, ILGPU) e `DirectX`
/// (v2.0, shader realtime); per ora solo `Cpu` è disponibile.
/// </summary>
public enum RenderEngine
{
    Cpu,
    Cuda,
    DirectX
}

public static class RenderEngineInfo
{
    public static bool IsAvailable(RenderEngine engine) => engine switch
    {
        RenderEngine.Cpu => true,
        RenderEngine.Cuda => GpuMandelbrot.IsReady,
        RenderEngine.DirectX => DxMandelbrot.IsReady,
        _ => false,
    };

    public static string DisplayName(RenderEngine engine) => engine switch
    {
        RenderEngine.Cuda => "CUDA",
        RenderEngine.DirectX => "DirectX",
        _ => "CPU",
    };
}
