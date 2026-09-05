using System.Drawing.Imaging;

namespace MandelbrotViewer;

/// <summary>Palette di colori disponibili nel visualizzatore.</summary>
public enum Palette
{
    Fuoco,
    Ghiaccio,
    Termico
}

/// <summary>
/// Logica di calcolo dell'insieme di Mandelbrot con smooth coloring.
/// z(n+1) = z(n)^2 + c, con z(0) = 0. Se |z| > 2 entro maxIter, c è fuori.
/// </summary>
public static class Mandelbrot
{
    /// <summary>
    /// Renderizza il frattale nel bitmap dato.
    /// </summary>
    /// <param name="bmp">Bitmap di destinazione (verrà sovrascritta).</param>
    /// <param name="centerX">Centro asse reale.</param>
    /// <param name="centerY">Centro asse immaginario.</param>
    /// <param name="scale">Larghezza del piano complesso visualizzata.</param>
    /// <param name="maxIter">Massimo numero di iterazioni.</param>
    /// <param name="palette">Palette di colori per i punti esterni.</param>
    /// <param name="supersample">Antialias: fattore k, calcola a risoluzione k volte
    /// maggiore e media ogni blocco kxk (1 = nessun antialias).</param>
    /// <param name="ct">Token per cancellare un rendering obsoleto.</param>
    public static void Render(Bitmap bmp, double centerX, double centerY, double scale, int maxIter, Palette palette, int supersample, CancellationToken ct)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        if (w <= 0 || h <= 0) return;

        int k = Math.Max(1, supersample);
        int bigW = w * k;
        int bigH = h * k;
        double pixelSize = scale / bigW;
        // Per mantenere le proporzioni, l'altezza complessa deriva dalla larghezza.
        double topY = centerY - (bigH * 0.5) * pixelSize;

        var rect = new Rectangle(0, 0, w, h);
        BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = data.Stride / 4; // int per riga
            int[] big = new int[bigW * bigH]; // colori a piena risoluzione (k=1: quella finale)

            Parallel.For(0, bigH, new ParallelOptions { CancellationToken = ct }, by =>
            {
                double cy = topY + by * pixelSize;
                for (int bx = 0; bx < bigW; bx++)
                {
                    double cx = centerX + (bx - bigW * 0.5) * pixelSize;

                    double zx = 0, zy = 0;
                    double zx2 = 0, zy2 = 0;
                    int iter = 0;

                    while (iter < maxIter && zx2 + zy2 <= 4.0)
                    {
                        zy = 2.0 * zx * zy + cy;
                        zx = zx2 - zy2 + cx;
                        zx2 = zx * zx;
                        zy2 = zy * zy;
                        iter++;
                    }

                    big[by * bigW + bx] = ColorFromEscape(iter, zx2 + zy2, maxIter, palette);
                }
            });

            // Downsample: media di ogni blocco kxk (con k=1 è l'identità).
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

    /// <summary>Media RGB di un blocco kxk del buffer a piena risoluzione.</summary>
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
    /// Colore da iterazioni e |z|² finali con smooth coloring.
    /// Usato sia dal percorso CPU sia per colorare i frame calcolati su GPU.
    /// </summary>
    internal static int ColorFromEscape(int iter, double mod2, int maxIter, Palette palette)
    {
        if (iter >= maxIter)
            return unchecked((int)0xFF000000); // dentro -> nero
        // Smooth coloring: mu evita le bande di colore nette.
        double modulus = Math.Sqrt(mod2);
        double mu = iter + 1 - Math.Log(Math.Log(modulus)) / Math.Log(2.0);
        if (double.IsNaN(mu) || double.IsInfinity(mu)) mu = iter;
        return ColorFor(mu, maxIter, palette);
    }

    // Gradiente (t, r, g, b) per ogni palette. t = iterazioni smussate / maxIter.
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

    /// <summary>Gradienti (t, r, g, b) della palette (5 stop da t=0 a t=1).</summary>
    internal static (double T, byte R, byte G, byte B)[] GetStops(Palette palette) => palette switch
    {
        Palette.Ghiaccio => IceStops,
        Palette.Termico => ThermalStops,
        _ => FireStops,
    };

    private static int ColorFor(double mu, int maxIter, Palette palette)
    {
        double t = Math.Clamp(mu / Math.Max(1, maxIter), 0.0, 1.0);
        t = Math.Pow(t, 0.65);
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

    /// <summary>
    /// Benchmark CPU: ripete il calcolo delle fughe (senza colorazione) per il budget
    /// dato e conta le iterazioni totali. Ritorna (iterazioni, secondi, frame).
    /// Il progresso alla UI è limitato (ogni 3 s) per non falsare la misura.
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

/// <summary>Avanzamento benchmark: secondi, iterazioni accumulate e frame completati.</summary>
public record struct BenchmarkProgress(double ElapsedSeconds, long TotalIters, int Frames)
{
    /// <summary>Intervallo minimo tra due aggiornamenti UI (ogni Invoke ruba tempo al test).</summary>
    public static readonly TimeSpan ReportInterval = TimeSpan.FromSeconds(3);
}
