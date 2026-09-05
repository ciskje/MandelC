namespace MandelbrotViewer;

public partial class Form1 : Form
{
    // Vista iniziale: tutto l'insieme (re in [-2.5, 1], im in [-1.2, 1.2]).
    private const double StartCenterX = -0.5;
    private const double StartCenterY = 0.0;
    private const double StartScale = 3.2; // larghezza in unità complesse

    private double _centerX = StartCenterX;
    private double _centerY = StartCenterY;
    private double _scale = StartScale;

    private Bitmap? _fractal;
    private CancellationTokenSource? _renderCts;
    private System.Windows.Forms.Timer _resizeTimer = null!;

    // Selezione rettangolare con trascinamento.
    private bool _selecting;
    private Point _selStart;
    private Point _selEnd;

    public Form1()
    {
        InitializeComponent();
        Text += $" v{AppVersion.Display}";

        _resizeTimer = new System.Windows.Forms.Timer { Interval = 300 };
        _resizeTimer.Tick += (s, e) => { _resizeTimer.Stop(); RenderAsync(); };

        Shown += (s, e) => RenderAsync();
    }

    private int MaxIter => (int)numIter.Value;

    // ---------- Rendering ----------

    private async void RenderAsync()
    {
        int w = Math.Max(1, pictureBox.Width);
        int h = Math.Max(1, pictureBox.Height);

        _renderCts?.Cancel();
        _renderCts?.Dispose();
        var cts = new CancellationTokenSource();
        _renderCts = cts;
        var token = cts.Token;

        double cx = _centerX, cy = _centerY, scale = _scale;
        int maxIter = MaxIter;

        lblStatus.Text = $"Calcolo {w}x{h}, iter={maxIter}...";
        Cursor = Cursors.WaitCursor;

        try
        {
            var bmp = new Bitmap(w, h);
            await Task.Run(() => Mandelbrot.Render(bmp, cx, cy, scale, maxIter, token), token);

            if (token.IsCancellationRequested) { bmp.Dispose(); return; }

            var old = _fractal;
            _fractal = bmp;
            pictureBox.Image?.Dispose();
            pictureBox.Image = _fractal;
            old?.Dispose();

            lblStatus.Text = $"Centro {cx:+0.000000;-0.000000} {cy:+0.000000;-0.000000}i | larghezza {scale:E2} | iter {maxIter}";
        }
        catch (OperationCanceledException) { /* rendering superato, ignora */ }
        finally
        {
            if (!token.IsCancellationRequested) Cursor = Cursors.Default;
        }
    }

    // ---------- Coordinate ----------

    private (double cx, double cy) PixelToComplex(Point p)
    {
        int w = pictureBox.Width, h = pictureBox.Height;
        double pixelSize = _scale / w;
        double cx = _centerX + (p.X - w * 0.5) * pixelSize;
        double cy = _centerY + (p.Y - h * 0.5) * pixelSize;
        return (cx, cy);
    }

    private void ZoomAt(Point p, double factor)
    {
        // Zoom centrato sul punto del mouse (factor < 1 = avvicina).
        var (cx, cy) = PixelToComplex(p);
        _centerX = cx + (_centerX - cx) * factor;
        _centerY = cy + (_centerY - cy) * factor;
        _scale *= factor;
        RenderAsync();
    }

    private void ZoomToRect(Rectangle r)
    {
        if (r.Width < 10 || r.Height < 10) return;
        // Centro del rettangolo diventa il nuovo centro; scala in base alla larghezza.
        var p1 = PixelToComplex(new Point(r.Left, r.Top));
        var p2 = PixelToComplex(new Point(r.Right, r.Bottom));
        _centerX = (p1.cx + p2.cx) / 2;
        _centerY = (p1.cy + p2.cy) / 2;
        _scale = Math.Abs(p2.cx - p1.cx);
        RenderAsync();
    }

    private void Reset()
    {
        _centerX = StartCenterX;
        _centerY = StartCenterY;
        _scale = StartScale;
        RenderAsync();
    }

    private void SavePng()
    {
        if (_fractal == null) return;
        using var dlg = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png",
            FileName = "mandelbrot.png"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _fractal.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
            lblStatus.Text = $"Salvato in {dlg.FileName}";
        }
    }

    // ---------- Eventi UI ----------

    private void BtnReset_Click(object? sender, EventArgs e) => Reset();

    private void BtnSave_Click(object? sender, EventArgs e) => SavePng();

    private void NumIter_ValueChanged(object? sender, EventArgs e) => RenderAsync();

    private void PictureBox_Resize(object? sender, EventArgs e)
    {
        _resizeTimer.Stop();
        _resizeTimer.Start(); // anti-rimbalzo: renderizza solo a resize finito
    }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        pictureBox.Focus();
        if (e.Button == MouseButtons.Left)
        {
            _selecting = true;
            _selStart = _selEnd = e.Location;
        }
        else if (e.Button == MouseButtons.Right)
        {
            ZoomAt(e.Location, 2.0); // tasto destro = allontana 2x
        }
    }

    private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_selecting)
        {
            _selEnd = e.Location;
            pictureBox.Invalidate(); // ridisegna rettangolo di selezione
        }
    }

    private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _selecting)
        {
            _selecting = false;
            var r = SelectionRect();
            pictureBox.Invalidate();
            ZoomToRect(r);
        }
    }

    private void PictureBox_MouseWheel(object? sender, MouseEventArgs e)
    {
        double factor = e.Delta > 0 ? 0.7 : 1.43; // rotella su = avvicina
        ZoomAt(e.Location, factor);
    }

    private void PictureBox_Paint(object? sender, PaintEventArgs e)
    {
        if (_selecting)
        {
            using var pen = new Pen(Color.White, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            e.Graphics.DrawRectangle(pen, SelectionRect());
        }
    }

    private Rectangle SelectionRect()
    {
        return new Rectangle(
            Math.Min(_selStart.X, _selEnd.X),
            Math.Min(_selStart.Y, _selEnd.Y),
            Math.Abs(_selEnd.X - _selStart.X),
            Math.Abs(_selEnd.Y - _selStart.Y));
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.R) Reset();
        else if (e.KeyCode == Keys.S) SavePng();
        else if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
            numIter.Value = Math.Min(numIter.Maximum, numIter.Value + 50);
        else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
            numIter.Value = Math.Max(numIter.Minimum, numIter.Value - 50);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _renderCts?.Cancel();
        base.OnFormClosing(e);
    }
}
