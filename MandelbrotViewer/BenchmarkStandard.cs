namespace MandelbrotViewer;

/// <summary>
/// Parametri standard del benchmark, identici per tutti i motori così i risultati
/// sono confrontabili: zona fissa ad alte iterazioni, 960x540 con AA 1x inteso come
/// griglia di campioni elementari senza media (960x540 pixel per frame), solo
/// conteggio delle iterazioni di fuga (niente colorazione né downsampling).
/// </summary>
internal static class BenchmarkStandard
{
    public const int Width = 960;
    public const int Height = 540;
    public const int Aa = 1;                    // griglia = Width*AA x Height*AA
    /// <summary>Iterazioni del benchmark, calcolate con la formula auto alla scala del test.</summary>
    public static int MaxIter => Mandelbrot.AutoIterForScale(Scale);
    public const double CenterX = -0.7499302568795561;
    public const double CenterY = -0.015139113925433963;
    public const double Scale = 0.00010453474311811176; // half = 5.226737155905588e-05
    public static readonly TimeSpan Budget = TimeSpan.FromSeconds(8);

    /// <summary>Campioni elementari per frame (Width*AA x Height*AA).</summary>
    public static long PixelsPerFrame => (long)Width * Height * Aa * Aa;

    /// <summary>Il metro finale: campioni elementari al secondo.</summary>
    public static double PixelsPerSecond(int frames, double seconds) =>
        seconds > 0 ? frames * PixelsPerFrame / seconds : 0;
}
