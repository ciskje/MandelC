using System.Drawing.Imaging;

namespace MandelbrotViewer;

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
    /// Colore da iterazioni e palette, condiviso dal percorso CPU. La formula dello
    /// smooth (log/log) e la mappatura sulla palette devono restare allineate con
    /// il kernel CUDA (GpuMandelbrot.cs) e lo shader HLSL (DxMandelbrot.cs).
    /// </summary>
    internal static int ColorFromEscape(int iter, double mod2, int maxIter, Palette palette)
    {
        if (iter >= maxIter)
            return unchecked((int)0xFF000000); // dentro -> nero
        double smoothIterations = iter + 1.0 - Math.Log(Math.Log(Math.Sqrt(Math.Max(mod2, 4.0)))) / Math.Log(2.0);
        return PaletteColors.ColorFor(smoothIterations, maxIter, palette);
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
