namespace MandelbrotViewer;

/// <summary>Avanzamento benchmark: secondi, iterazioni accumulate e frame completati.
/// TotalIters è significativo solo per il benchmark CPU: CUDA e DirectX misurano a
/// frame (il lavoro per frame è fisso), quindi riportano TotalIters = 0.</summary>
public record struct BenchmarkProgress(double ElapsedSeconds, long TotalIters, int Frames)
{
    /// <summary>Intervallo minimo tra due aggiornamenti UI (ogni Invoke ruba tempo al test).</summary>
    public static readonly TimeSpan ReportInterval = TimeSpan.FromSeconds(1);
}
