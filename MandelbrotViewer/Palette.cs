using System.Runtime.CompilerServices;

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
    // Single source of stops for the CPU and DirectX paths; the CUDA kernel samples
    // the same table entries uploaded to the device (GpuMandelbrot.EnsureLut),
    // DirectX samples them from a texture in the Graded function of the HLSL shader
    // (DxMandelbrot.cs). The three implementations MUST stay aligned:
    // same entries and same mapping t = (nu/maxIter)^0.35 (gamma, like the
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

    // Fast lookup table size for the render hot loop. Entries are linearly
    // interpolated on lookup, so 4096 steps show no visible banding while the
    // per-pixel cost stays far below Pow + stop search.
    internal const int LutSize = 4096;

    private static readonly object LutGate = new();
    private static int[]? _cachedLut;
    private static Palette _cachedPalette = (Palette)(-1);
    private static int _cachedMaxIter = -1;

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
    // the device table sampled by LutColor in Cuda/mandelbrot.cu and with Graded in the HLSL shader).
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

    // Returns the cached fast color table for the render hot loop.
    // Param palette (Palette): Input: palette whose stops are baked into the table.
    // Param maxIter (int): Input: iteration budget baked into the table normalization.
    // Returns (int[]): Output: shared cached array of LutSize packed ARGB colors.
    //   Entry i holds the color for raw=(i+0.5)/LutSize with t=raw^0.35. Do not mutate.
    internal static int[] GetLut(Palette palette, int maxIter)
    {
        lock (LutGate)
        {
            if (_cachedLut != null && _cachedPalette == palette && _cachedMaxIter == maxIter)
                return _cachedLut;
            var stops = GetStops(palette);
            // Flatten stops to channels: all palettes use even spacing, so segment = t*4
            // exactly like the CUDA/HLSL paths (no T search in the hot loop).
            float s0r = stops[0].R, s0g = stops[0].G, s0b = stops[0].B;
            float s1r = stops[1].R, s1g = stops[1].G, s1b = stops[1].B;
            float s2r = stops[2].R, s2g = stops[2].G, s2b = stops[2].B;
            float s3r = stops[3].R, s3g = stops[3].G, s3b = stops[3].B;
            float s4r = stops[4].R, s4g = stops[4].G, s4b = stops[4].B;
            var lut = new int[LutSize];
            for (int i = 0; i < LutSize; i++)
            {
                double raw = (i + 0.5) / LutSize;
                double t = Math.Pow(raw, 0.35);
                double segment = t * 4.0;
                int seg = segment >= 3.0 ? 3 : (int)segment;
                double f = segment - seg;
                float ar = seg == 0 ? s0r : seg == 1 ? s1r : seg == 2 ? s2r : s3r;
                float ag = seg == 0 ? s0g : seg == 1 ? s1g : seg == 2 ? s2g : s3g;
                float ab = seg == 0 ? s0b : seg == 1 ? s1b : seg == 2 ? s2b : s3b;
                float br = seg == 0 ? s1r : seg == 1 ? s2r : seg == 2 ? s3r : s4r;
                float bg = seg == 0 ? s1g : seg == 1 ? s2g : seg == 2 ? s3g : s4g;
                float bb = seg == 0 ? s1b : seg == 1 ? s2b : seg == 2 ? s3b : s4b;
                int r = (int)(ar + f * (br - ar));
                int g = (int)(ag + f * (bg - ag));
                int b = (int)(ab + f * (bb - ab));
                lut[i] = unchecked((int)(0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | (uint)b));
            }
            _cachedLut = lut;
            _cachedPalette = palette;
            _cachedMaxIter = maxIter;
            return lut;
        }
    }

    // Maps a smoothed iteration value through the fast table with linear
    // interpolation between adjacent entries (no visible banding).
    // Param lut (int[]): Input: table from GetLut (same palette/maxIter as the view).
    // Param smooth (double): Input: smoothed escape value nu in [0, maxIter].
    // Param invMaxIter (double): Input: 1.0/maxIter, precomputed by the caller.
    // Returns (int): Output: packed ARGB color interpolated from the table.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ColorFromLut(int[] lut, double smooth, double invMaxIter)
    {
        double pos = smooth * invMaxIter * lut.Length - 0.5;
        if (pos <= 0.0) return lut[0];
        if (pos >= lut.Length - 1) return lut[lut.Length - 1];
        int i0 = (int)pos;
        double f = pos - i0;
        int c0 = lut[i0], c1 = lut[i0 + 1];
        double r = ((c0 >> 16) & 0xFF) + f * ((((c1 >> 16) & 0xFF) - ((c0 >> 16) & 0xFF)));
        double g = ((c0 >> 8) & 0xFF) + f * ((((c1 >> 8) & 0xFF) - ((c0 >> 8) & 0xFF)));
        double b = (c0 & 0xFF) + f * (((c1 & 0xFF) - (c0 & 0xFF)));
        return unchecked((int)(0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | (uint)b));
    }
}
