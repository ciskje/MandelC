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
    /// <param name="ct">Token per cancellare un rendering obsoleto.</param>
    public static void Render(Bitmap bmp, double centerX, double centerY, double scale, int maxIter, CancellationToken ct)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        if (w <= 0 || h <= 0) return;

        double pixelSize = scale / w;
        // Per mantenere le proporzioni, l'altezza complessa deriva dalla larghezza.
        double topY = centerY - (h * 0.5) * pixelSize;

        var rect = new Rectangle(0, 0, w, h);
        BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = data.Stride / 4; // int per riga
            int[] pixels = new int[stride * h];

            Parallel.For(0, h, new ParallelOptions { CancellationToken = ct }, y =>
            {
                double cy = topY + y * pixelSize;
                for (int x = 0; x < w; x++)
                {
                    double cx = centerX + (x - w * 0.5) * pixelSize;

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

                    int color;
                    if (iter >= maxIter)
                    {
                        color = unchecked((int)0xFF000000); // dentro -> nero
                    }
                    else
                    {
                        // Smooth coloring: mu evita le bande di colore nette.
                        double modulus = Math.Sqrt(zx2 + zy2);
                        double mu = iter + 1 - Math.Log(Math.Log(modulus)) / Math.Log(2.0);
                        if (double.IsNaN(mu) || double.IsInfinity(mu)) mu = iter;
                        color = ColorFor(mu, maxIter);
                    }
                    pixels[y * stride + x] = color;
                }
            });

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    private static int ColorFor(double mu, int maxIter)
    {
        // Tonalità ciclica sul numero di iterazioni smussato.
        double t = mu / maxIter;
        double hue = (360.0 * Math.Pow(t, 0.6) + 200.0) % 360.0;
        double sat = 0.85;
        double val = mu < 1 ? 0.4 + 0.6 * mu : 1.0;
        HsvToRgb(hue, sat, val, out int r, out int g, out int b);
        return unchecked((int)(0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | (uint)b));
    }

    private static void HsvToRgb(double h, double s, double v, out int r, out int g, out int b)
    {
        double c = v * s;
        double hh = h / 60.0;
        double x = c * (1 - Math.Abs(hh % 2 - 1));
        double r1 = 0, g1 = 0, b1 = 0;
        int sector = ((int)Math.Floor(hh) % 6 + 6) % 6;
        switch (sector)
        {
            case 0: r1 = c; g1 = x; break;
            case 1: r1 = x; g1 = c; break;
            case 2: g1 = c; b1 = x; break;
            case 3: g1 = x; b1 = c; break;
            case 4: r1 = x; b1 = c; break;
            default: r1 = c; b1 = x; break;
        }
        double m = v - c;
        r = (int)(255 * (r1 + m));
        g = (int)(255 * (g1 + m));
        b = (int)(255 * (b1 + m));
    }
}
