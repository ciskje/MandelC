namespace MandelbrotViewer;

// Color palettes available in the viewer (the order is
// the dropdown index and is persisted in settings.json: add only at the end).
public enum Palette
{
    Fire,
    Ice,
    Thermal,
    Ocean,
    Violet,
    Desert,
    Forest
}

    // Palette gradients and iteration-based color interpolation.
    // Single source of stops for the CPU and DirectX paths; the CUDA kernel receives the
    // same stops via GpuPaletteParams and interpolates them in
    // GpuMandelbrot.ColorFromIterations, DirectX in the Graded function of the HLSL shader
    // (DxMandelbrot.cs). The three implementations MUST stay aligned:
    // same 5 stops and same mapping t = (nu/maxIter)^0.35 (gamma, like the
    // Python reference: smooth iteration + tone curve).
internal static class PaletteColors
{
    // Gradient (t, r, g, b) for each palette. t = iterations / maxIter.
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

    // Violet: 2 opposite and vivid base colors, magenta and cyan (the inside stays black).
    private static readonly (double T, byte R, byte G, byte B)[] VioletStops =
    [
        (0.00, 60, 0, 80),
        (0.25, 210, 0, 190),
        (0.50, 120, 40, 220),
        (0.75, 0, 200, 230),
        (1.00, 210, 255, 255),
    ];

    private static readonly (double T, byte R, byte G, byte B)[] DesertStops =
    [
        (0.00, 0, 0, 0),
        (0.25, 95, 50, 10),
        (0.50, 185, 110, 40),
        (0.75, 235, 190, 110),
        (1.00, 255, 245, 220),
    ];

    // Forest: 2 base colors, brown and green at the extremes (the inside stays black).
    private static readonly (double T, byte R, byte G, byte B)[] ForestStops =
    [
        (0.00, 55, 32, 12),
        (0.25, 115, 78, 32),
        (0.50, 95, 125, 45),
        (0.75, 60, 170, 70),
        (1.00, 205, 235, 175),
    ];

    // Gradients (t, r, g, b) of the palette (5 stops from t=0 to t=1).
    internal static (double T, byte R, byte G, byte B)[] GetStops(Palette palette) => palette switch
    {
        Palette.Ice => IceStops,
        Palette.Thermal => ThermalStops,
        Palette.Ocean => OceanStops,
        Palette.Violet => VioletStops,
        Palette.Desert => DesertStops,
        Palette.Forest => ForestStops,
        _ => FireStops,
    };

    // Color interpolation for the CPU path (aligned with
    // GpuMandelbrot.ColorFromIterations and with Graded in the HLSL shader).
    // Mapping: t = (nu/maxIter)^0.35 (gamma, smooth iteration already applied
    // by the caller).
    internal static int ColorFor(double iterations, int maxIter, Palette palette)
    {
        double t = Math.Pow(Math.Clamp(iterations / Math.Max(1, maxIter), 0.0, 1.0), 0.35);
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
