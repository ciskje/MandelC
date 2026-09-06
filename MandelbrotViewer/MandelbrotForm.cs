using System.Text.Json;

namespace MandelbrotViewer;

public partial class MandelbrotForm : Form
{
    // Vista iniziale: tutto l'insieme (re in [-2.5, 1], im in [-1.2, 1.2]).
    private const double StartCenterX = -0.5;
    private const double StartCenterY = 0.0;
    private const double StartScale = 3.2; // larghezza in unità complesse

    private double _centerX = StartCenterX;
    private double _centerY = StartCenterY;
    private double _scale = StartScale;
    private RenderEngine _engine = RenderEngine.Cpu;
    private AppSettings _settings = new();
    private bool _suspendRender = true; // true finché il costruttore applica le impostazioni
    private string? _gpuSelection;      // scheda video scelta (null = auto)

    private Bitmap? _fractal;
    private CancellationTokenSource? _renderCts;
    private System.Windows.Forms.Timer _resizeTimer = null!;
    private System.Windows.Forms.Timer _dxTimer = null!;
    private bool _dxDirty = true; // prossimo frame DirectX da ridisegnare

    // Click = zoom, trascinamento = pan (spostamento).
    private const int DragThresholdPx = 5; // sotto: è un click, sopra: è un trascinamento
    private const int PanThrottleMs = 80;  // ricampiona il pan al massimo ogni 80 ms
    private bool _dragging;
    private MouseButtons _dragButton;
    private Point _downPos;   // dove è stato premuto il pulsante
    private Point _lastPos;   // ultima posizione durante il trascinamento
    private bool _moved;      // true se superata la soglia di trascinamento
    private int _lastPanTick; // Environment.TickCount dell'ultimo render di pan

    public MandelbrotForm()
    {
        InitializeComponent();
        Text += $" v{AppVersion.Display}";

        _resizeTimer = new System.Windows.Forms.Timer { Interval = 300 };
        _resizeTimer.Tick += (s, e) => { _resizeTimer.Stop(); RenderAsync(); };

        _dxTimer = new System.Windows.Forms.Timer { Interval = 16 }; // loop realtime ~60fps
        _dxTimer.Tick += DxTimer_Tick;
        _dxTimer.Start();

        LoadSettings();
        _suspendRender = false;

        Shown += async (s, e) =>
        {
            // Dropdown GPU: unione delle schede DirectX e dei device CUDA.
            var dxNames = DxMandelbrot.AdapterNames();
            var cudaNames = await Task.Run(GpuMandelbrot.DeviceNames);
            foreach (string n in dxNames.Concat(cudaNames).Distinct().OrderBy(n => n))
                if (!cmbGpu.Items.Contains(n)) cmbGpu.Items.Add(n);

            string savedGpu = _settings.Gpu;
            if (!string.IsNullOrEmpty(savedGpu) && cmbGpu.Items.Contains(savedGpu))
                cmbGpu.SelectedItem = savedGpu;
            _gpuSelection = cmbGpu.SelectedIndex > 0 ? cmbGpu.SelectedItem!.ToString() : null;

            bool dxOk = DxMandelbrot.TryInitialize(dxPanel.Handle, Math.Max(1, dxPanel.Width), Math.Max(1, dxPanel.Height), _gpuSelection);
            if (dxOk) radioDx.Enabled = true;
            if (_settings.Engine == nameof(RenderEngine.DirectX) && dxOk)
                radioDx.Checked = true;
            ApplyEngineVisibility(); // primo paint (bitmap, o DX se preselezionato)

            bool gpu = await Task.Run(() => GpuMandelbrot.TryInitialize(_gpuSelection));
            if (gpu && !IsDisposed)
            {
                radioCuda.Enabled = true;
                // Usa la GPU se è la preferenza salvata (default per nuove installazioni).
                if (_settings.Engine != nameof(RenderEngine.Cpu)
                    && _settings.Engine != nameof(RenderEngine.DirectX))
                    radioCuda.Checked = true;
            }

            // Se un motore GPU non è disponibile, mostra il motivo (prima restava solo grigio).
            var problemi = new List<string>();
            if (!dxOk) problemi.Add("DirectX: " + (DxMandelbrot.LastError.Length > 0 ? DxMandelbrot.LastError : "inizializzazione non riuscita"));
            if (!gpu) problemi.Add("CUDA: " + (GpuMandelbrot.LastError.Length > 0 ? GpuMandelbrot.LastError : "nessun device disponibile"));
            if (problemi.Count > 0 && !IsDisposed)
                lblStatus.Text = string.Join("   |   ", problemi);
        };
    }

    private int MaxIter => chkIterAuto.Checked ? AutoIter() : (int)numIter.Value;

    /// <summary>Fattore antialias dal dropdown (1x = disabilitato, 2x/4x/8x = attivo).</summary>
    private int AaFactor => cmbAA.SelectedIndex > 0 ? 1 << cmbAA.SelectedIndex : 1;

    private Palette ActivePalette => cmbPalette.SelectedIndex < 0
        ? Palette.Fuoco
        : (Palette)cmbPalette.SelectedIndex;

    /// <summary>Precisione CUDA scelta dall'utente: 64-bit (double) se radio 64, altrimenti 32-bit (float).</summary>
    private bool UseDoublePrecision => radPrec64.Checked;

    private int AutoIter()
    {
        // Più si ingrandisce, più iterazioni servono per bordi nitidi.
        // Tarata per ~2000 iterazioni a scala 1,95e-4 e ~4550 a scala 1e-5.
        double zoom = StartScale / Math.Max(double.Epsilon, _scale);
        int iter = (int)(200 + 790 * Math.Log10(Math.Max(1.0, zoom)));
        return Math.Clamp(iter, (int)numIter.Minimum, (int)numIter.Maximum);
    }

    // ---------- Rendering ----------

    /// <param name="preview">True durante il trascinamento: niente AA e 1/4 dei pixel.</param>
    private async void RenderAsync(bool preview = false)
    {
        if (_suspendRender) return;
        int fullW = Math.Max(1, pictureBox.Width);
        int fullH = Math.Max(1, pictureBox.Height);
        // Anteprima veloce: metà per lato (= un quarto dei pixel), senza antialias.
        int w = preview ? Math.Max(1, fullW / 2) : fullW;
        int h = preview ? Math.Max(1, fullH / 2) : fullH;

        _renderCts?.Cancel();
        _renderCts?.Dispose();
        var cts = new CancellationTokenSource();
        _renderCts = cts;
        var token = cts.Token;

        double cx = _centerX, cy = _centerY, scale = _scale;
        int maxIter = MaxIter;
        Palette palette = ActivePalette;
        int aa = preview ? 1 : AaFactor;
        bool useCuda = _engine == RenderEngine.Cuda && GpuMandelbrot.IsReady;
        bool gpuDouble = false;
        if (chkIterAuto.Checked)
            numIter.Value = maxIter; // in auto il numero è disabilitato ma mostra il valore usato

        lblStatus.Text = $"Calcolo {(preview ? "anteprima " : "")}{w}x{h}, iter={maxIter}...";
        SetBusyCursor(true);

        try
        {
            var bmp = new Bitmap(w, h);
            if (useCuda)
                gpuDouble = await Task.Run(() => GpuMandelbrot.Render(bmp, cx, cy, scale, maxIter, palette, aa, UseDoublePrecision, token), token);
            else
                await Task.Run(() => Mandelbrot.Render(bmp, cx, cy, scale, maxIter, palette, aa, token), token);

            if (preview && (bmp.Width != fullW || bmp.Height != fullH))
                bmp = Upscale(bmp, fullW, fullH);

            if (token.IsCancellationRequested) { bmp.Dispose(); return; }

            var old = _fractal;
            _fractal = bmp;
            pictureBox.Image = _fractal;
            old?.Dispose();

            string engineLabel = useCuda ? $"CUDA-{(gpuDouble ? "double" : "float")} {GpuMandelbrot.DeviceShortName}" : "CPU";
            lblStatus.Text = $"Centro {cx:+0.000000;-0.000000} {cy:+0.000000;-0.000000}i | larghezza {scale:E2} | iter {maxIter}{(chkIterAuto.Checked ? " (auto)" : "")} | {ActivePalette}{(aa > 1 ? $" AA{aa}x" : "")} | motore {engineLabel}{(preview ? " (anteprima)" : "")}";
        }
        catch (OperationCanceledException) { /* rendering superato, ignora */ }
        finally
        {
            if (!token.IsCancellationRequested) SetBusyCursor(false);
        }
    }

    /// <summary>
    /// Imposta (o ripristina) il cursore di occupato su form e tutti i discendenti
    /// (ricorsivo: copre anche pictureBox, menu e stato). È AppStarting
    /// (freccia+clessidra) e non Wait perché durante il render async l'UI resta
    /// interattiva (pan/zoom annullano e rilanciano il calcolo); `UseWaitCursor`
    /// non si può usare perché forza la clessidra piena.
    /// </summary>
    private void SetBusyCursor(bool busy)
    {
        ApplyCursorRecursive(this, busy ? Cursors.AppStarting : Cursors.Default);
    }

    private static void ApplyCursorRecursive(Control root, Cursor cursor)
    {
        root.Cursor = cursor;
        foreach (Control child in root.Controls)
            ApplyCursorRecursive(child, cursor);
    }

    /// <summary>Ingrandisce il bitmap di anteprima a piena risoluzione (bilineare).</summary>
    private static Bitmap Upscale(Bitmap small, int fullW, int fullH)
    {
        var up = new Bitmap(fullW, fullH);
        using (var g = Graphics.FromImage(up))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            g.DrawImage(small, 0, 0, fullW, fullH);
        }
        small.Dispose();
        return up;
    }

    /// <summary>Ridisegna: realtime se motore DirectX, altrimenti render bitmap (eventuale anteprima).</summary>
    private void InvalidateView(bool preview = false)
    {
        if (_engine == RenderEngine.DirectX && DxMandelbrot.IsReady)
            _dxDirty = true;
        else
            RenderAsync(preview);
    }

    private void ApplyEngineVisibility()
    {
        bool dx = _engine == RenderEngine.DirectX && DxMandelbrot.IsReady;
        dxPanel.Visible = dx;
        pictureBox.Visible = !dx;
        bool gpuEngine = _engine != RenderEngine.Cpu; // il dropdown GPU si mostra solo con un motore GPU
        lblGpu.Visible = gpuEngine;
        cmbGpu.Visible = gpuEngine;
        bool prec = _engine == RenderEngine.Cuda; // la scelta di precisione vale solo per CUDA
        radPrec32.Enabled = prec;
        radPrec64.Enabled = prec;
        if (dx)
            _dxDirty = true;
        else
            RenderAsync();
    }

    private void DxTimer_Tick(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        if (_engine != RenderEngine.DirectX || !DxMandelbrot.IsReady) return;
        if (dxPanel.Width <= 0 || dxPanel.Height <= 0) return;
        try
        {
            DxMandelbrot.Resize(dxPanel.Width, dxPanel.Height);
            if (!_dxDirty) return;
            _dxDirty = false;
            DxMandelbrot.Render(_centerX, _centerY, _scale, dxPanel.Width, dxPanel.Height, MaxIter, AaFactor, ActivePalette);
            UpdateDxStatus();
        }
        catch (Exception ex)
        {
            FallbackToCpu("DirectX: " + ex.Message);
        }
    }

    private void UpdateDxStatus()
    {
        string warn = GpuMandelbrot.WantsDouble(_scale) ? " [oltre float!]" : "";
        lblStatus.Text = $"Centro {_centerX:+0.000000;-0.000000} {_centerY:+0.000000;-0.000000}i | larghezza {_scale:E2} | iter {MaxIter}{(chkIterAuto.Checked ? " (auto)" : "")} | {ActivePalette} | motore DirectX (float){warn}";
    }

    private void FallbackToCpu(string reason)
    {
        DxMandelbrot.Dispose();
        radioDx.Enabled = false;
        if (_engine == RenderEngine.DirectX)
        {
            _engine = RenderEngine.Cpu;
            radioCpu.Checked = true;
        }
        ApplyEngineVisibility();
        lblStatus.Text = reason + " — passo a CPU";
    }

    // ---------- Coordinate ----------

    private (double cx, double cy) PixelToComplex(Point p)
    {
        Size vs = ActiveViewSize;
        int w = vs.Width, h = vs.Height;
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
        InvalidateView();
    }

    /// <summary>Dimensioni della vista attiva (pannello DirectX o pictureBox bitmap).</summary>
    private Size ActiveViewSize =>
        (_engine == RenderEngine.DirectX && DxMandelbrot.IsReady) ? dxPanel.Size : pictureBox.Size;

    private void PanBy(Point from, Point to)
    {
        // Sposta la vista seguendo il mouse: il punto sotto il cursore resta sotto il cursore.
        double pixelSize = _scale / Math.Max(1, ActiveViewSize.Width);
        _centerX += (from.X - to.X) * pixelSize;
        _centerY += (from.Y - to.Y) * pixelSize;
    }

    private void Reset()
    {
        _centerX = StartCenterX;
        _centerY = StartCenterY;
        _scale = StartScale;
        InvalidateView();
    }

    private void SavePng()
    {
        Bitmap? captured = null;
        try
        {
            Bitmap? src = (_engine == RenderEngine.DirectX && DxMandelbrot.IsReady)
                ? (captured = DxMandelbrot.Capture()) // dal backbuffer DirectX
                : _fractal;
            if (src == null) return;
            using var dlg = new SaveFileDialog
            {
                Filter = "PNG (*.png)|*.png",
                FileName = "mandelbrot.png"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                src.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                lblStatus.Text = $"Salvato in {dlg.FileName}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Salvataggio fallito:\n{ex.Message}",
                "Salva PNG", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            captured?.Dispose();
        }
    }

    // ---------- Zone (salvataggio/caricamento vista in JSON) ----------

    /// <summary>Vista salvata: centro, larghezza complessa e iterazioni.</summary>
    private sealed record ViewZone(double CenterX, double CenterY, double Scale, int MaxIter);

    private void SaveZone()
    {
        var zone = new ViewZone(_centerX, _centerY, _scale, MaxIter);
        using var dlg = new SaveFileDialog
        {
            Filter = "Zona Mandelbrot (*.json)|*.json",
            FileName = "zona.json",
            DefaultExt = "json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        string json = JsonSerializer.Serialize(zone, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dlg.FileName, json);
        lblStatus.Text = $"Zona salvata in {dlg.FileName}";
    }

    private void LoadZone()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Zona Mandelbrot (*.json)|*.json",
            DefaultExt = "json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            string json = File.ReadAllText(dlg.FileName);
            var zone = JsonSerializer.Deserialize<ViewZone>(json);
            if (zone == null || !double.IsFinite(zone.CenterX) || !double.IsFinite(zone.CenterY)
                || !double.IsFinite(zone.Scale) || zone.Scale <= 0)
            {
                throw new InvalidDataException("Il file non contiene una zona valida.");
            }
            _centerX = zone.CenterX;
            _centerY = zone.CenterY;
            _scale = zone.Scale;
            chkIterAuto.Checked = false; // la zona salva iterazioni esplicite
            numIter.Value = Math.Clamp(zone.MaxIter, (int)numIter.Minimum, (int)numIter.Maximum);
            InvalidateView();
            lblStatus.Text = $"Zona caricata da {dlg.FileName}";
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
        {
            MessageBox.Show(this, $"Impossibile caricare la zona:\n{ex.Message}",
                "Carica zona", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(this,
            $"Visualizzatore Insieme di Mandelbrot v{AppVersion.Display}\n" +
            $"Motore: {EngineDescription()}\n\n" +
            "Esplora il frattale z = z² + c con smooth coloring.\n\n" +
            "Click sinistro: avvicina 2x sul punto\n" +
            "Click destro: allontana 2x sul punto\n" +
            "Rotella: zoom sul cursore\n" +
            "Trascinamento: sposta la vista\n" +
            "R: reset | S: salva PNG | +/-: iterazioni\n" +
            "Palette: Fuoco, Ghiaccio o Termico dal menu a tendina\n" +
            "Iterazioni Auto: checkbox, crescono con l'ingrandimento\n" +
            "AA: 1x off, 2x/4x/8x con media dei pixel vicini\n" +
            "Motori: CPU, CUDA (compute) o DirectX (realtime, float)\n" +
            "GPU: scegli la scheda video dal menu a tendina (Auto = più potente)\n\n" +
            "Menu File: salva/carica la zona in formato JSON.",
            "Informazioni", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private string EngineDescription() => _engine switch
    {
        RenderEngine.DirectX when DxMandelbrot.IsReady => "DirectX realtime (float)",
        RenderEngine.Cuda when GpuMandelbrot.IsReady => $"CUDA ({GpuMandelbrot.DeviceShortName})",
        _ => "CPU multicore",
    };

    /// <summary>Applica le impostazioni salvate (senza renderizzare: ci pensa lo Shown).</summary>
    private void LoadSettings()
    {
        _settings = AppSettings.Load();
        // La vista non si memorizza: si parte sempre dall'insieme completo.
        numIter.Value = Math.Clamp(_settings.MaxIter, (int)numIter.Minimum, (int)numIter.Maximum);
        chkIterAuto.Checked = _settings.IterAuto;
        cmbPalette.SelectedIndex = Math.Clamp(_settings.Palette, 0, cmbPalette.Items.Count - 1);
        cmbAA.SelectedIndex = Math.Clamp(_settings.AaIndex, 0, cmbAA.Items.Count - 1);
        radPrec32.Checked = _settings.Single; // 32-bit se Single, altrimenti resta 64-bit
        _gpuSelection = string.IsNullOrEmpty(_settings.Gpu) ? null : _settings.Gpu;

        var r = new Rectangle(_settings.WinX, _settings.WinY, _settings.WinW, _settings.WinH);
        if (r.Width > 0 && r.Height > 0 && Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(r)))
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = r;
        }
        if (_settings.Maximized) WindowState = FormWindowState.Maximized;
    }

    private void SaveSettings()
    {
        try
        {
            var r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            _settings.IterAuto = chkIterAuto.Checked;
            _settings.MaxIter = (int)numIter.Value;
            _settings.Palette = cmbPalette.SelectedIndex;
            _settings.AaIndex = cmbAA.SelectedIndex;
            _settings.Single = radPrec32.Checked; // true = 32-bit (float)
            _settings.Engine = _engine.ToString();
            _settings.Gpu = _gpuSelection ?? "";
            _settings.WinX = r.X;
            _settings.WinY = r.Y;
            _settings.WinW = r.Width;
            _settings.WinH = r.Height;
            _settings.Maximized = WindowState == FormWindowState.Maximized;
            _settings.Save();
        }
        catch
        {
            // Mai bloccare la chiusura per le impostazioni.
        }
    }

    // ---------- Eventi UI ----------

    private void BtnReset_Click(object? sender, EventArgs e) => Reset();

    private void SaveImageItem_Click(object? sender, EventArgs e) => SavePng(); // menu File → "Salva immagine..." (Ctrl+Shift+S)

    private void SaveZoneItem_Click(object? sender, EventArgs e) => SaveZone();

    private void LoadZoneItem_Click(object? sender, EventArgs e) => LoadZone();

    private void AboutItem_Click(object? sender, EventArgs e) => ShowAbout();

    private void LogItem_Click(object? sender, EventArgs e)
    {
        var dlg = new LogForm(BuildDiagnosticLog()) { Owner = this };
        dlg.ShowDialog(this);
    }

    private string BuildDiagnosticLog()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Visualizzatore Insieme di Mandelbrot   v{AppVersion.Full}");
        sb.AppendLine($"Aperto il: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"OS: {Environment.OSVersion.VersionString}");
        sb.AppendLine($"Runtime .NET: {Environment.Version}");
        sb.AppendLine();
        sb.AppendLine("=== Motore ===");
        sb.AppendLine($"Selezionato: {RenderEngineInfo.DisplayName(_engine)}");
        sb.AppendLine($"In uso:     {EngineDescription()}");
        sb.AppendLine();
        sb.AppendLine("=== DirectX (D3D11) ===");
        sb.AppendLine($"Pronto:        {(DxMandelbrot.IsReady ? "sì" : "NO")}");
        sb.AppendLine($"Scheda in uso: {(DxMandelbrot.AdapterName.Length > 0 ? DxMandelbrot.AdapterName : "(nessuna)")}");
        sb.AppendLine($"Ultimo errore: {(DxMandelbrot.LastError.Length > 0 ? DxMandelbrot.LastError : "(nessuno)")}");
        sb.AppendLine($"Schede DXGI:   {JoinOrNone(DxMandelbrot.AdapterNames())}");
        sb.AppendLine($"Enum DXGI errore: {(DxMandelbrot.EnumerationError.Length > 0 ? DxMandelbrot.EnumerationError : "(nessuno)")}");
        sb.AppendLine();
        sb.AppendLine("=== CUDA (ILGPU) ===");
        sb.AppendLine($"Pronto:         {(GpuMandelbrot.IsReady ? "sì" : "NO")}");
        sb.AppendLine($"Device in uso:  {(GpuMandelbrot.DeviceName.Length > 0 ? GpuMandelbrot.DeviceName : "(nessuno)")}");
        sb.AppendLine($"Ultimo errore:  {(GpuMandelbrot.LastError.Length > 0 ? GpuMandelbrot.LastError : "(nessuno)")}");
        sb.AppendLine($"Device CUDA:    {JoinOrNone(GpuMandelbrot.DeviceNames())}");
        sb.AppendLine();
        sb.AppendLine("=== Selezione GPU ===");
        sb.AppendLine($"Scheda scelta:   {(_gpuSelection ?? "Auto")}");
        sb.AppendLine($"Precisione CUDA: {(UseDoublePrecision ? "64-bit (double)" : "32-bit (float)")}");
        sb.AppendLine();
        sb.AppendLine("=== Impostazioni salvate ===");
        sb.AppendLine($"Motore:     {_settings.Engine}");
        sb.AppendLine($"GPU:        {(_settings.Gpu.Length > 0 ? _settings.Gpu : "Auto")}");
        sb.AppendLine($"Palette:    {_settings.Palette}    AA: {_settings.AaIndex}    Iterazioni: {_settings.MaxIter} (auto = {_settings.IterAuto})");
        sb.AppendLine($"Precisione (Single): {_settings.Single}");
        return sb.ToString();
    }

    private static string JoinOrNone(IReadOnlyList<string> items)
        => items.Count > 0 ? string.Join(",  ", items) : "(nessuna)";

    private void BenchmarkItem_Click(object? sender, EventArgs e)
    {
        bool pauseDirectX = _engine == RenderEngine.DirectX && DxMandelbrot.IsReady;
        if (pauseDirectX)
        {
            _dxTimer.Stop();
            // Durante il benchmark la swapchain mostra il frame grigio dei campioni:
            // nasconde il pannello DX per non confondere la vista principale.
            dxPanel.Visible = false;
        }
        try
        {
            using var dlg = new BenchmarkForm(_engine, UseDoublePrecision);
            dlg.ShowDialog(this);
        }
        finally
        {
            if (pauseDirectX)
            {
                dxPanel.Visible = true;
                _dxDirty = true;
                _dxTimer.Start();
            }
        }
    }

    private void NumIter_ValueChanged(object? sender, EventArgs e)
    {
        if (!chkIterAuto.Checked) InvalidateView(); // in auto le iterazioni le decide lo zoom
    }

    private void ChkIterAuto_CheckedChanged(object? sender, EventArgs e)
    {
        numIter.Enabled = !chkIterAuto.Checked;
        InvalidateView();
    }

    private void CmbAA_SelectedIndexChanged(object? sender, EventArgs e) => InvalidateView();

    private void CmbPalette_SelectedIndexChanged(object? sender, EventArgs e) => InvalidateView();

    private void PrecRadio_CheckedChanged(object? sender, EventArgs e) => InvalidateView();

    /// <summary>Etichetta della GPU selezionata nel dropdown ("Auto" se indice 0).</summary>
    private string GpuLabel() => cmbGpu.SelectedIndex > 0 ? cmbGpu.SelectedItem?.ToString() ?? "Auto" : "Auto";

    private void CmbGpu_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbGpu.SelectedIndex < 0) return;
        _gpuSelection = cmbGpu.SelectedIndex > 0 ? cmbGpu.SelectedItem!.ToString() : null;
        string name = GpuLabel();

        if (_engine == RenderEngine.DirectX)
        {
            if (DxMandelbrot.TryInitialize(dxPanel.Handle, Math.Max(1, dxPanel.Width), Math.Max(1, dxPanel.Height), _gpuSelection))
            {
                _dxDirty = true;
                lblStatus.Text = $"GPU: {name} (DirectX)";
            }
            else
            {
                FallbackToCpu($"DirectX su '{name}': {DxMandelbrot.LastError}");
            }
        }
        else if (_engine == RenderEngine.Cuda)
        {
            if (GpuMandelbrot.TryInitialize(_gpuSelection))
            {
                InvalidateView();
                lblStatus.Text = $"GPU: {name} (CUDA {GpuMandelbrot.DeviceShortName})";
            }
            else
            {
                _engine = RenderEngine.Cpu;
                radioCpu.Checked = true;
                ApplyEngineVisibility();
                lblStatus.Text = $"CUDA su '{name}': {GpuMandelbrot.LastError} — passo a CPU";
            }
        }
        else
        {
            // Motore CPU: selezione ricordata, si applica al cambio di motore.
            lblStatus.Text = $"GPU: {name} (motore CPU, si applicherà al cambio motore)";
        }
    }

    private void EngineRadio_CheckedChanged(object? sender, EventArgs e)
    {
        if (sender is not RadioButton r || !r.Checked) return;
        _engine = r == radioCuda ? RenderEngine.Cuda
            : r == radioDx ? RenderEngine.DirectX
            : RenderEngine.Cpu;

        // Motore GPU ancora non inizializzato: lo inizializza adesso sulla scheda scelta.
        if (_engine == RenderEngine.Cuda && !GpuMandelbrot.IsReady)
        {
            Cursor = Cursors.WaitCursor;
            bool ok = GpuMandelbrot.TryInitialize(_gpuSelection);
            Cursor = Cursors.Default;
            if (!ok)
            {
                _engine = RenderEngine.Cpu;
                radioCpu.Checked = true;
                ApplyEngineVisibility();
                lblStatus.Text = "CUDA non disponibile: " + GpuMandelbrot.LastError;
                return;
            }
        }
        if (_engine == RenderEngine.DirectX && !DxMandelbrot.IsReady)
        {
            if (!DxMandelbrot.TryInitialize(dxPanel.Handle, Math.Max(1, dxPanel.Width), Math.Max(1, dxPanel.Height), _gpuSelection))
            {
                radioDx.Enabled = false;
                _engine = RenderEngine.Cpu;
                radioCpu.Checked = true;
                ApplyEngineVisibility();
                lblStatus.Text = "DirectX non disponibile: " + DxMandelbrot.LastError;
                return;
            }
        }

        if (!RenderEngineInfo.IsAvailable(_engine)) _engine = RenderEngine.Cpu;
        ApplyEngineVisibility();
    }

    private void ExitItem_Click(object? sender, EventArgs e) => Close();

    private void PictureBox_Resize(object? sender, EventArgs e)
    {
        _resizeTimer.Stop();
        _resizeTimer.Start(); // anti-rimbalzo: renderizza solo a resize finito
    }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        ((Control?)sender)?.Focus();
        if (e.Button is MouseButtons.Left or MouseButtons.Right)
        {
            _dragging = true;
            _dragButton = e.Button;
            _downPos = _lastPos = e.Location;
            _moved = false;
        }
    }

    private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        if (!_moved && Distance(_downPos, e.Location) < DragThresholdPx) return; // ancora un possibile click

        _moved = true;
        PanBy(_lastPos, e.Location);
        _lastPos = e.Location;

        // Throttle: evita di accodare un render per ogni pixel di movimento.
        int now = Environment.TickCount;
        if (now - _lastPanTick >= PanThrottleMs)
        {
            _lastPanTick = now;
            InvalidateView(preview: true); // bitmap: anteprima; DirectX: frame pieno realtime
        }
    }

    private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_dragging || e.Button != _dragButton) return;
        _dragging = false;

        if (!_moved)
        {
            // Click senza trascinamento = zoom centrato sul punto.
            if (e.Button == MouseButtons.Left)
                ZoomAt(e.Location, 0.5); // avvicina 2x
            else
                ZoomAt(e.Location, 2.0); // allontana 2x
        }
        else
        {
            InvalidateView(); // assicura il disegno della posizione finale del pan
        }
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void PictureBox_MouseWheel(object? sender, MouseEventArgs e)
    {
        double factor = e.Delta > 0 ? 0.7 : 1.43; // rotella su = avvicina, centrata sul mouse
        ZoomAt(e.Location, factor);
    }

    private void PictureBox_MouseEnter(object? sender, EventArgs e)
    {
        // La rotella arriva al controllo con il focus: lo prende al passaggio del mouse.
        if (sender is Control c && !c.Focused) c.Focus();
    }

    private void MandelbrotForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control || e.Alt) return; // le combinazioni Ctrl+/Alt+ spettano ai menu
        if (e.KeyCode == Keys.R) Reset();
        else if (e.KeyCode == Keys.S) SavePng();
        else if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
            numIter.Value = Math.Min(numIter.Maximum, numIter.Value + 50);
        else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
            numIter.Value = Math.Max(numIter.Minimum, numIter.Value - 50);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveSettings();
        _dxTimer.Stop();
        _renderCts?.Cancel();
        DxMandelbrot.Dispose();
        GpuMandelbrot.Dispose();
        base.OnFormClosing(e);
    }
}
