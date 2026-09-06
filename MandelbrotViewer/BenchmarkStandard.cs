namespace MandelbrotViewer;

/// <summary>
/// Parametri standard del benchmark, identici per tutti i motori così i risultati
/// sono confrontabili: zona fissa ad alte iterazioni, 960x540 con AA 8x inteso come
/// griglia di campioni elementari senza media (7680x4320 pixel per frame), solo
/// conteggio delle iterazioni di fuga (niente colorazione né downsampling).
/// </summary>
internal static class BenchmarkStandard
{
    public const int Width = 960;
    public const int Height = 540;
    public const int Aa = 8;                    // griglia = Width*AA x Height*AA
    public const int MaxIter = 5000;
    public const double CenterX = -0.743643887037151; // valle dei cavallucci marini
    public const double CenterY = 0.131825904205330;
    public const double Scale = 0.0005;
    public static readonly TimeSpan Budget = TimeSpan.FromSeconds(8);

    /// <summary>Campioni elementari per frame (Width*AA x Height*AA).</summary>
    public static long PixelsPerFrame => (long)Width * Height * Aa * Aa;

    /// <summary>Il metro finale: campioni elementari al secondo.</summary>
    public static double PixelsPerSecond(int frames, double seconds) =>
        seconds > 0 ? frames * PixelsPerFrame / seconds : 0;
}
