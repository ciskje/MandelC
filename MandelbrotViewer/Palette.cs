namespace MandelbrotViewer;

/// <summary>Palette di colori disponibili nel visualizzatore (l'ordine è
/// l'indice del dropdown ed è persistito in settings.json: aggiungere solo in coda).</summary>
public enum Palette
{
    Fuoco,
    Ghiaccio,
    Termico,
    Oceano,
    Viola,
    Deserto,
    Foresta
}

/// <summary>
/// Gradienti della palette e interpolazione del colore basata sulle iterazioni.
/// Fonte unica degli stop per i percorsi CPU e DirectX; il kernel CUDA riceve gli
/// stessi stop tramite <see cref="GpuPaletteParams"/> e li interpola in
/// GpuMandelbrot.ColorFromIterations, DirectX nella funzione Graded dello shader
/// HLSL (DxMandelbrot.cs). Le tre implementazioni DEVONO restare allineate:
/// stessi 5 stop e stessa mappatura t = iter/maxIter * 1.35 + 0.03.
/// </summary>
internal static class PaletteColors
{
    // Gradiente (t, r, g, b) per ogni palette. t = iterazioni / maxIter.
    private static readonly (double T, byte R, byte G, byte B)[] FireStops =
    [
        (0.00, 0, 0, 0),
        (0.25, 170, 20, 0),
        (0.50, 255, 120, 0),
        (0.75, 255, 220, 90),
        (1.00, 255, 255, 225),
    ];

    private static readonly (double T, byte R, byte G, byte B)[] IceStops =
    [
        (0.00, 0, 0, 0),
        (0.25, 0, 35, 110),
        (0.50, 0, 130, 220),
        (0.75, 130, 215, 255),
        (1.00, 240, 255, 255),
    ];

    private static readonly (double T, byte R, byte G, byte B)[] ThermalStops =
    [
        (0.00, 0, 0, 0),
        (0.25, 75, 10, 105),
        (0.50, 195, 25, 85),
        (0.75, 250, 135, 30),
        (1.00, 252, 250, 180),
    ];

    private static readonly (double T, byte R, byte G, byte B)[] OceanStops =
    [
        (0.00, 0, 0, 0),
        (0.25, 0, 40, 95),
        (0.50, 0, 120, 175),
        (0.75, 85, 200, 225),
        (1.00, 235, 250, 255),
    ];

    private static readonly (double T, byte R, byte G, byte B)[] VioletStops =
    [
        (0.00, 0, 0, 0),
        (0.25, 48, 0, 95),
        (0.50, 125, 20, 175),
        (0.75, 205, 85, 220),
        (1.00, 245, 220, 255),
    ];

    private static readonly (double T, byte R, byte G, byte B)[] DesertStops =
    [
        (0.00, 0, 0, 0),
        (0.25, 95, 50, 10),
        (0.50, 185, 110, 40),
        (0.75, 235, 190, 110),
        (1.00, 255, 245, 220),
    ];

    private static readonly (double T, byte R, byte G, byte B)[] ForestStops =
    [
        (0.00, 0, 0, 0),
        (0.25, 0, 70, 20),
        (0.50, 20, 150, 60),
        (0.75, 140, 230, 120),
        (1.00, 240, 255, 220),
    ];

    /// <summary>Gradienti (t, r, g, b) della palette (5 stop da t=0 a t=1).</summary>
    internal static (double T, byte R, byte G, byte B)[] GetStops(Palette palette) => palette switch
    {
        Palette.Ghiaccio => IceStops,
        Palette.Termico => ThermalStops,
        Palette.Oceano => OceanStops,
        Palette.Viola => VioletStops,
        Palette.Deserto => DesertStops,
        Palette.Foresta => ForestStops,
        _ => FireStops,
    };

    /// <summary>Interpolazione del colore per il percorso CPU (allineata con
    /// GpuMandelbrot.ColorFromIterations e con Graded nello shader HLSL).</summary>
    internal static int ColorFor(double iterations, int maxIter, Palette palette)
    {
        double t = Math.Clamp(iterations / Math.Max(1, maxIter) * 1.35 + 0.03, 0.0, 1.0);
        var stops = GetStops(palette);

        var (t0, r0, g0, b0) = stops[0];
        for (int i = 1; i < stops.Length; i++)
        {
            var (t1, r1, g1, b1) = stops[i];
            if (t <= t1)
            {
                double f = (t - t0) / Math.Max(double.Epsilon, t1 - t0);
                int r = (int)(r0 + f * (r1 - r0));
                int g = (int)(g0 + f * (g1 - g0));
                int b = (int)(b0 + f * (b1 - b0));
                return unchecked((int)(0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | (uint)b));
            }
            (t0, r0, g0, b0) = (t1, r1, g1, b1);
        }
        var last = stops[^1];
        return unchecked((int)(0xFF000000u | ((uint)last.R << 16) | ((uint)last.G << 8) | last.B));
    }
}
