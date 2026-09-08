namespace MandelbrotViewer;

// Avanzamento benchmark: secondi, iterazioni accumulate e frame completati.
// TotalIters è significativo solo per il benchmark CPU: CUDA e DirectX misurano a
// frame (il lavoro per frame è fisso), quindi riportano TotalIters = 0.
public record struct BenchmarkProgress(double ElapsedSeconds, long TotalIters, int Frames)
{
    // Intervallo minimo tra due aggiornamenti UI (ogni Invoke ruba tempo al test).
    public static readonly TimeSpan ReportInterval = TimeSpan.FromSeconds(1);
}
