using System.Drawing.Imaging;

namespace MandelbrotViewer;

// Computation logic of the Mandelbrot (and Julia) set with smooth coloring.
// Mandelbrot: z(n+1) = z(n)^2 + c, with z(0) = 0. If |z| > 2 within maxIter, c is outside.
// Julia: same iteration but z(0) = pixel point and c = fixed constant.
public static class Mandelbrot
{
    // Renders the fractal into the given bitmap (whole-frame path, double precision).
    // Thin wrapper over RenderTile with zero offsets and full size equal
    // to the bitmap size; kept so interactive view and video frames need no tiling math.
    // Runs on a worker thread via Parallel.For with cancellation support.
    // Param bmp (Bitmap): Input/output: destination bitmap, overwritten in place via LockBits
    //   (Format32bppArgb). Input pixels are ignored; output pixels receive the SSAA-averaged
    //   ARGB colors of the requested view. Must be non-empty.
    // Param centerX (double): Input: view center on the real axis (complex units).
    // Param centerY (double): Input: view center on the imaginary axis (complex units).
    // Param scale (double): Input: complex width of the view. The complex height derives
    //   from it via the bitmap aspect ratio, so the image never stretches.
    // Param maxIter (int): Input: escape-iteration budget per subsample (clamped 50…50000
    //   by the UI; auto mode derives it from the zoom via AutoIterForScale).
    //   Points still bounded after maxIter render black (interior).
    // Param palette (Palette): Input: color palette for exterior points (5-stop gradient).
    // Param supersample (int): Input: antialias factor k (1 = off). Each output pixel
    //   averages k×k subsamples (cost ~k²). Computed on-chip per output pixel, without
    //   allocating a k-times buffer.
    // Param ct (CancellationToken): Input: cancellation token. Observed between rows; an obsolete render
    //   (pan/zoom superseded) aborts with OperationCanceledException and leaves bmp untouched
    //   past UnlockBits.
    // Param juliaCx (double): Input: constant c, real part (Julia mode only).
    // Param juliaCy (double): Input: constant c, imaginary part (Julia mode only).
    // Param julia (bool): Input: mode switch. True = Julia set (z(0) = pixel, c fixed);
    //   false = Mandelbrot (z(0) = 0, c = pixel).
    public static void Render(Bitmap bmp, double centerX, double centerY, double scale, int maxIter, Palette palette, int supersample, CancellationToken ct, double juliaCx = 0, double juliaCy = 0, bool julia = false)
    {
        RenderTile(bmp, centerX, centerY, scale, maxIter, palette, supersample, ct,
            0, 0, bmp.Width, bmp.Height, juliaCx, juliaCy, julia);
    }

    // Renders one output tile using the coordinate system of the complete image.
    // On-chip SSAA: each output pixel computes its k×k subsamples and accumulates
    // them locally, so memory stays O(W×H) and no W*k×H*k buffer is allocated.
    // Sample coordinates use the global k-times grid, so tiled output is
    // pixel-identical to a whole-frame render at the same view and AA.
    // Param bmp (Bitmap): Input/output: tile bitmap, overwritten in place via LockBits.
    //   Output pixels receive the SSAA-averaged ARGB colors of the tile region.
    // Param centerX (double): Input: whole-image view center, real axis (complex units).
    // Param centerY (double): Input: whole-image view center, imaginary axis (complex units).
    // Param scale (double): Input: complex width of the whole image (not of the tile).
    // Param maxIter (int): Input: escape-iteration budget per subsample; interior
    //   (iter ≥ maxIter) renders black.
    // Param palette (Palette): Input: color palette for exterior points.
    // Param supersample (int): Input: antialias factor k (1 = off, identity average).
    // Param ct (CancellationToken): Input: cancellation token, observed by the Parallel.For rows.
    // Param offsetX (int): Input: tile origin X in whole-image output pixels (columns skipped).
    // Param offsetY (int): Input: tile origin Y in whole-image output pixels (rows skipped).
    // Param fullWidth (int): Input: whole-image width in output pixels (defines pixelSize
    //   together with scale, and the global grid fullWidth*k).
    // Param fullHeight (int): Input: whole-image height in output pixels (defines the
    //   complex height via aspect, and the global grid fullHeight*k).
    // Param juliaCx (double): Input: constant c, real part (Julia mode only).
    // Param juliaCy (double): Input: constant c, imaginary part (Julia mode only).
    // Param julia (bool): Input: mode switch (true = Julia, false = Mandelbrot).
    public static void RenderTile(Bitmap bmp, double centerX, double centerY, double scale, int maxIter, Palette palette,
        int supersample, CancellationToken ct, int offsetX, int offsetY, int fullWidth, int fullHeight,
        double juliaCx = 0, double juliaCy = 0, bool julia = false)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        if (w <= 0 || h <= 0) return;

        int k = Math.Max(1, supersample);
        int fullBigW = fullWidth * k;
        int fullBigH = fullHeight * k;
        double pixelSize = scale / fullBigW;
        // To keep the aspect ratio, the complex height derives from the width.
        double topY = centerY - (fullBigH * 0.5) * pixelSize;
        double originX = centerX - (fullBigW * 0.5) * pixelSize;
        int samples = k * k;

        var rect = new Rectangle(0, 0, w, h);
        BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = data.Stride / 4; // ints per row
            int[] pixels = new int[stride * h];

            // Same sample coordinates as the former full-buffer path
            // (globalBx/globalBy on the k-times grid), so tiled output stays
            // pixel-identical to a whole-frame render.
            Parallel.For(0, h, new ParallelOptions { CancellationToken = ct }, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    long r = 0, g = 0, b = 0;
                    for (int sy = 0; sy < k; sy++)
                    {
                        int globalBy = offsetY * k + y * k + sy;
                        double py = topY + globalBy * pixelSize;
                        for (int sx = 0; sx < k; sx++)
                        {
                            int globalBx = offsetX * k + x * k + sx;
                            double px = originX + globalBx * pixelSize;

                            // Julia: z(0) = pixel point, c = constant; Mandelbrot: z(0) = 0, c = pixel.
                            double zx = julia ? px : 0, zy = julia ? py : 0;
                            double ccx = julia ? juliaCx : px, ccy = julia ? juliaCy : py;
                            double zx2 = zx * zx, zy2 = zy * zy;
                            int iter = 0;

                            while (iter < maxIter && zx2 + zy2 <= 4.0)
                            {
                                zy = 2.0 * zx * zy + ccy;
                                zx = zx2 - zy2 + ccx;
                                zx2 = zx * zx;
                                zy2 = zy * zy;
                                iter++;
                            }

                            int c = ColorFromEscape(iter, zx2 + zy2, maxIter, palette);
                            r += (c >> 16) & 0xFF;
                            g += (c >> 8) & 0xFF;
                            b += c & 0xFF;
                        }
                    }
                    pixels[y * stride + x] = unchecked((int)(0xFF000000u | ((uint)(r / samples) << 16) | ((uint)(g / samples) << 8) | (uint)(b / samples)));
                }
            });

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    // Color from iterations and palette, shared by the CPU path. The smooth
    // (log/log) formula and the palette mapping must stay aligned with
    // the CUDA kernel (GpuMandelbrot.cs) and the HLSL shader (DxMandelbrot.cs).
    // Called once per subsample; the caller averages the channels.
    // Param iter (int): Input: raw escape-iteration count of the subsample
    //   (0 … maxIter). Values ≥ maxIter mean interior.
    // Param mod2 (double): Input: squared modulus |z|² at escape (≥ 4 for exterior points;
    //   clamped inside). Feeds the fractional log/log smoothing term.
    // Param maxIter (int): Input: iteration budget of the view; normalizes the smoothed
    //   value before the palette lookup.
    // Param palette (Palette): Input: color palette for exterior points.
    // Returns (int): Output: packed 32-bit ARGB color (alpha 0xFF). Interior (iter ≥ maxIter)
    //   returns opaque black; exterior returns the smoothed, gamma-mapped palette color.
    internal static int ColorFromEscape(int iter, double mod2, int maxIter, Palette palette)
    {
        if (iter >= maxIter)
            return unchecked((int)0xFF000000); // inside -> black
        double smoothIterations = iter + 1.0 - Math.Log(Math.Log(Math.Sqrt(Math.Max(mod2, 4.0)))) / Math.Log(2.0);
        return PaletteColors.ColorFor(smoothIterations, maxIter, palette);
    }

    // Automatic iterations based on the zoom: 2000 for the initial view
    // (half side = 1.5) plus 2000 every 10x, i.e. 2000 * (1 + log10(1.5 / half)),
    // with half = scale / 2 (half of the visible width). Clamp 50-50000.
    // Single source for the view, the zoom video and the benchmark zone value.
    // Param scale (double): Input: complex width of the view. Smaller scale (deeper zoom)
    //   yields more iterations; non-positive scales are guarded via double.Epsilon.
    // Returns (int): Output: iteration budget in [50, 50000] (~1944 at the initial view
    //   scale 9.36, 10915 at the benchmark zone).
    public static int AutoIterForScale(double scale)
    {
        double half = Math.Max(double.Epsilon, scale * 0.5);
        double iter = 2000.0 * (1.0 + Math.Log10(1.5 / half));
        if (iter < 50.0) return 50;
        if (iter > 50000.0) return 50000;
        return (int)iter;
    }

    // CPU benchmark: repeats the escape computation (without coloring) for the given
    // budget and counts the total iterations.
    // The progress to the UI is limited (every 3 s) to not skew the measure.
    // Each grid cell is one elementary sample (no SSAA averaging); the metric is
    // frames × samples/frame / seconds, comparable across engines.
    // Param centerX: Input: benchmark zone center, real axis (complex units).
    // Param centerY: Input: benchmark zone center, imaginary axis (complex units).
    // Param scale: Input: complex width of the benchmark zone.
    // Param w: Input: grid width in elementary samples (before supersample).
    // Param h: Input: grid height in elementary samples (before supersample).
    // Param maxIter: Input: iteration budget per sample (auto formula at the zone scale).
    // Param supersample: Input: grid multiplier k; the measured grid is w*k × h*k
    //   (standard uses 1).
    // Param budget: Input: time budget per run (8 s standard); frames repeat until elapsed.
    // Param progress: Input (nullable): UI progress sink, reported at most every
    //   ReportInterval plus a final report; null disables reporting.
    // Param ct: Input: cancellation token; checked each frame and by the Parallel.For.
    // Returns: Output: tuple (TotalIters = summed escape iterations over all frames,
    //   Seconds = effective elapsed seconds, Frames = completed frames).
    public static (long TotalIters, double Seconds, int Frames) BenchmarkCpu(double centerX, double centerY, double scale, int w, int h, int maxIter, int supersample, TimeSpan budget, IProgress<BenchmarkProgress>? progress, CancellationToken ct)
    {
        int k = Math.Max(1, supersample);
        int bigW = w * k;
        int bigH = h * k;
        double pixelSize = scale / bigW;
        double topY = centerY - (bigH * 0.5) * pixelSize;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        long total = 0;
        int frames = 0;
        bool first = true;
        TimeSpan lastReport = TimeSpan.Zero;

        while (sw.Elapsed < budget)
        {
            ct.ThrowIfCancellationRequested();
            long frame = 0;
            object gate = new();

            Parallel.For(0, bigH, new ParallelOptions { CancellationToken = ct }, () => 0L,
                (y, state, local) =>
                {
                    double cy = topY + y * pixelSize;
                    for (int x = 0; x < bigW; x++)
                    {
                        double cx = centerX + (x - bigW * 0.5) * pixelSize;
                        double zx = 0, zy = 0, zx2 = 0, zy2 = 0;
                        int iter = 0;
                        while (iter < maxIter && zx2 + zy2 <= 4.0)
                        {
                            zy = 2.0 * zx * zy + cy;
                            zx = zx2 - zy2 + cx;
                            zx2 = zx * zx;
                            zy2 = zy * zy;
                            iter++;
                        }
                        local += iter;
                    }
                    return local;
                },
                local => { lock (gate) frame += local; });

            total += frame;
            frames++;
            if (first || sw.Elapsed - lastReport >= BenchmarkProgress.ReportInterval)
            {
                progress?.Report(new BenchmarkProgress(sw.Elapsed.TotalSeconds, total, frames));
                lastReport = sw.Elapsed;
                first = false;
            }
        }

        progress?.Report(new BenchmarkProgress(sw.Elapsed.TotalSeconds, total, frames));
        return (total, sw.Elapsed.TotalSeconds, frames);
    }
}
