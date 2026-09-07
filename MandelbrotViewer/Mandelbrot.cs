using System.Drawing.Imaging;

namespace MandelbrotViewer;

/// <summary>
/// Computation logic of the Mandelbrot (and Julia) set with smooth coloring.
/// Mandelbrot: z(n+1) = z(n)^2 + c, with z(0) = 0. If |z| > 2 within maxIter, c is outside.
/// Julia: same iteration but z(0) = pixel point and c = fixed constant.
/// </summary>
public static class Mandelbrot
{
    /// <summary>
    /// Renders the fractal into the given bitmap.
    /// </summary>
    /// <param name="bmp">Destination bitmap (will be overwritten).</param>
    /// <param name="centerX">Center of the real axis.</param>
    /// <param name="centerY">Center of the imaginary axis.</param>
    /// <param name="scale">Width of the complex plane displayed.</param>
    /// <param name="maxIter">Maximum number of iterations.</param>
    /// <param name="palette">Color palette for the external points.</param>
    /// <param name="supersample">Antialias: factor k, computes at a resolution k times
    /// greater and averages each kxk block (1 = no antialias).</param>
    /// <param name="ct">Token to cancel an obsolete rendering.</param>
    /// <param name="juliaCx">Constant c (real part) in Julia mode.</param>
    /// <param name="juliaCy">Constant c (imaginary part) in Julia mode.</param>
    /// <param name="julia">True = Julia set with fixed c, false = Mandelbrot.</param>
    public static void Render(Bitmap bmp, double centerX, double centerY, double scale, int maxIter, Palette palette, int supersample, CancellationToken ct, double juliaCx = 0, double juliaCy = 0, bool julia = false)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        if (w <= 0 || h <= 0) return;

        int k = Math.Max(1, supersample);
        int bigW = w * k;
        int bigH = h * k;
        double pixelSize = scale / bigW;
        // To keep the aspect ratio, the complex height derives from the width.
        double topY = centerY - (bigH * 0.5) * pixelSize;

        var rect = new Rectangle(0, 0, w, h);
        BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = data.Stride / 4; // ints per row
            int[] big = new int[bigW * bigH]; // full-resolution colors (k=1: the final one)

            Parallel.For(0, bigH, new ParallelOptions { CancellationToken = ct }, by =>
            {
                double py = topY + by * pixelSize;
                for (int bx = 0; bx < bigW; bx++)
                {
                    double px = centerX + (bx - bigW * 0.5) * pixelSize;

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

                    big[by * bigW + bx] = ColorFromEscape(iter, zx2 + zy2, maxIter, palette);
                }
            });

            // Downsample: average of each kxk block (with k=1 it is the identity).
            int[] pixels = new int[stride * h];
            Parallel.For(0, h, new ParallelOptions { CancellationToken = ct }, y =>
            {
                for (int x = 0; x < w; x++)
                    pixels[y * stride + x] = AverageBlock(big, bigW, x * k, y * k, k);
            });

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    /// <summary>RGB average of a kxk block of the full-resolution buffer.</summary>
    internal static int AverageBlock(int[] big, int bigW, int x0, int y0, int k)
    {
        long r = 0, g = 0, b = 0;
        for (int dy = 0; dy < k; dy++)
        {
            int row = (y0 + dy) * bigW + x0;
            for (int dx = 0; dx < k; dx++)
            {
                int c = big[row + dx];
                r += (c >> 16) & 0xFF;
                g += (c >> 8) & 0xFF;
                b += c & 0xFF;
            }
        }
        int n = k * k;
        return unchecked((int)(0xFF000000u | ((uint)(r / n) << 16) | ((uint)(g / n) << 8) | (uint)(b / n)));
    }

    /// <summary>
    /// Color from iterations and palette, shared by the CPU path. The smooth
    /// (log/log) formula and the palette mapping must stay aligned with
    /// the CUDA kernel (GpuMandelbrot.cs) and the HLSL shader (DxMandelbrot.cs).
    /// </summary>
    internal static int ColorFromEscape(int iter, double mod2, int maxIter, Palette palette)
    {
        if (iter >= maxIter)
            return unchecked((int)0xFF000000); // inside -> black
        double smoothIterations = iter + 1.0 - Math.Log(Math.Log(Math.Sqrt(Math.Max(mod2, 4.0)))) / Math.Log(2.0);
        return PaletteColors.ColorFor(smoothIterations, maxIter, palette);
    }

    /// <summary>
    /// Automatic iterations based on the zoom: 2000 for the initial view
    /// (half side = 1.5) plus 2000 every 10x, i.e. 2000 * (1 + log10(1.5 / half)),
    /// with half = scale / 2 (half of the visible width). Clamp 50-50000.
    /// </summary>
    public static int AutoIterForScale(double scale)
    {
        double half = Math.Max(double.Epsilon, scale * 0.5);
        double iter = 2000.0 * (1.0 + Math.Log10(1.5 / half));
        if (iter < 50.0) return 50;
        if (iter > 50000.0) return 50000;
        return (int)iter;
    }

    /// <summary>
    /// CPU benchmark: repeats the escape computation (without coloring) for the given
    /// budget and counts the total iterations. Returns (iterations, seconds, frames).
    /// The progress to the UI is limited (every 3 s) to not skew the measure.
    /// </summary>
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
