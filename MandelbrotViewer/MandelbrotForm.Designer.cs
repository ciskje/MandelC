namespace MandelbrotViewer;

    partial class MandelbrotForm
{
    private System.ComponentModel.IContainer components = null!;
    private PictureBox pictureBox = null!;
    private Panel dxPanel = null!;
    private Panel topPanel = null!;
    private FlowLayoutPanel row0 = null!;
    private FlowLayoutPanel row1 = null!;
    private Button btnReset = null!;
    private Button btnTorna = null!;
    private Button btnBenchmark = null!;
    private Label lblIter = null!;
    private NumericUpDown numIter = null!;
    private CheckBox chkIterAuto = null!;
    private Label lblPalette = null!;
    private ComboBox cmbPalette = null!;
    private Label lblAA = null!;
    private ComboBox cmbAA = null!;
    private Label lblEngine = null!;
    private Label lblGpu = null!;
    private ComboBox cmbGpu = null!;
    private Label lblPrec = null!;
    private FlowLayoutPanel precisionPanel = null!;
    private RadioButton radPrec32 = null!;
    private RadioButton radPrec64 = null!;
    private RadioButton radioCpu = null!;
    private RadioButton radioCuda = null!;
    private RadioButton radioDx = null!;
    private FlowLayoutPanel enginePanel = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel lblStatus = null!;
    private ToolStripStatusLabel statusHelp = null!;
    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem fileMenu = null!;
    private ToolStripMenuItem loadZoneItem = null!;
    private ToolStripMenuItem saveZoneItem = null!;
    private ToolStripSeparator fileSeparator = null!;
    private ToolStripMenuItem saveImageItem = null!;
    private ToolStripMenuItem generaMenu = null!;
    private ToolStripMenuItem exportItem = null!;
    private ToolStripMenuItem videoItem = null!;
    private ToolStripMenuItem realTimeItem = null!;
    private ToolStripSeparator fileSeparator2 = null!;
    private ToolStripMenuItem benchmarkItem = null!;
    private ToolStripSeparator fileSeparator3 = null!;
    private ToolStripMenuItem exitItem = null!;
    private ToolStripMenuItem helpMenu = null!;
    private ToolStripMenuItem aboutItem = null!;
    private ToolStripSeparator helpSeparator = null!;
    private ToolStripMenuItem logItem = null!;
    private System.Windows.Forms.ToolTip toolTip = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.pictureBox = new PictureBox();
        this.dxPanel = new Panel();
        this.topPanel = new Panel();
        this.row0 = new FlowLayoutPanel();
        this.row1 = new FlowLayoutPanel();
        this.btnReset = new Button();
        this.btnTorna = new Button();
        this.btnBenchmark = new Button();
        this.lblIter = new Label();
        this.numIter = new NumericUpDown();
        this.chkIterAuto = new CheckBox();
        this.lblPalette = new Label();
        this.cmbPalette = new ComboBox();
        this.lblAA = new Label();
        this.cmbAA = new ComboBox();
        this.lblEngine = new Label();
        this.lblGpu = new Label();
        this.cmbGpu = new ComboBox();
        this.lblPrec = new Label();
        this.precisionPanel = new FlowLayoutPanel();
        this.radPrec32 = new RadioButton();
        this.radPrec64 = new RadioButton();
        this.radioCpu = new RadioButton();
        this.radioCuda = new RadioButton();
        this.radioDx = new RadioButton();
        this.enginePanel = new FlowLayoutPanel();
        this.statusStrip = new StatusStrip();
        this.lblStatus = new ToolStripStatusLabel();
        this.statusHelp = new ToolStripStatusLabel();
        this.menuStrip = new MenuStrip();
        this.fileMenu = new ToolStripMenuItem();
        this.loadZoneItem = new ToolStripMenuItem();
        this.saveZoneItem = new ToolStripMenuItem();
        this.fileSeparator = new ToolStripSeparator();
        this.saveImageItem = new ToolStripMenuItem();
        this.generaMenu = new ToolStripMenuItem();
        this.exportItem = new ToolStripMenuItem();
        this.videoItem = new ToolStripMenuItem();
        this.realTimeItem = new ToolStripMenuItem();
        this.fileSeparator2 = new ToolStripSeparator();
        this.benchmarkItem = new ToolStripMenuItem();
        this.fileSeparator3 = new ToolStripSeparator();
        this.exitItem = new ToolStripMenuItem();
        this.helpMenu = new ToolStripMenuItem();
        this.aboutItem = new ToolStripMenuItem();
        this.helpSeparator = new ToolStripSeparator();
        this.logItem = new ToolStripMenuItem();
        this.toolTip = new System.Windows.Forms.ToolTip();

        ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numIter)).BeginInit();
        this.topPanel.SuspendLayout();
        this.row0.SuspendLayout();
        this.row1.SuspendLayout();
        this.enginePanel.SuspendLayout();
        this.precisionPanel.SuspendLayout();
        this.statusStrip.SuspendLayout();
        this.menuStrip.SuspendLayout();
        this.SuspendLayout();

        // topPanel: due righe FlowLayoutPanel, controlli ammassati a sinistra
        this.topPanel.Dock = DockStyle.Top;
        this.topPanel.Height = 72;
        this.topPanel.Padding = new Padding(8, 4, 8, 4);
        this.topPanel.Controls.Add(this.row0);
        this.topPanel.Controls.Add(this.row1);

        // Riga 0
        this.row0.Dock = DockStyle.Top;
        this.row0.AutoSize = true;
        this.row0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.row0.WrapContents = false;
        this.row0.Margin = new Padding(0);
        this.row0.Padding = new Padding(0);
        this.row0.Controls.Add(this.btnReset);
        this.row0.Controls.Add(this.btnTorna);
        this.row0.Controls.Add(this.btnBenchmark);
        this.row0.Controls.Add(this.lblIter);
        this.row0.Controls.Add(this.numIter);
        this.row0.Controls.Add(this.chkIterAuto);
        this.row0.Controls.Add(this.lblPalette);
        this.row0.Controls.Add(this.cmbPalette);
        this.row0.Controls.Add(this.lblAA);
        this.row0.Controls.Add(this.cmbAA);

        // Riga 1
        this.row1.Dock = DockStyle.Top;
        this.row1.AutoSize = true;
        this.row1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.row1.WrapContents = false;
        this.row1.Margin = new Padding(0, 2, 0, 0);
        this.row1.Padding = new Padding(0);
        this.row1.Controls.Add(this.lblEngine);
        this.row1.Controls.Add(this.enginePanel);
        this.row1.Controls.Add(this.lblPrec);
        this.row1.Controls.Add(this.precisionPanel);
        this.row1.Controls.Add(this.lblGpu);
        this.row1.Controls.Add(this.cmbGpu);

        // Margini minimi tra controlli
        foreach (Control ctrl in this.row0.Controls)
            ctrl.Margin = new Padding(3, 1, 3, 1);
        foreach (Control ctrl in this.row1.Controls)
            ctrl.Margin = new Padding(3, 1, 3, 1);

        // btnReset
        this.btnReset.Anchor = AnchorStyles.Left;
        this.btnReset.Size = new System.Drawing.Size(60, 23);
        this.btnReset.Text = "Reset";
        this.btnReset.Click += new EventHandler(this.BtnReset_Click);

        // btnTorna
        this.btnTorna.Anchor = AnchorStyles.Left;
        this.btnTorna.Size = new System.Drawing.Size(60, 23);
        this.btnTorna.Text = "Torna";
        this.btnTorna.Click += new EventHandler(this.RealTimeItem_Click);

        // btnBenchmark (il salvataggio PNG resta nel menu File)
        this.btnBenchmark.Anchor = AnchorStyles.Left;
        this.btnBenchmark.Size = new System.Drawing.Size(100, 23);
        this.btnBenchmark.Text = "Benchmark";
        this.btnBenchmark.Click += new EventHandler(this.BenchmarkItem_Click);

        // lblIter
        this.lblIter.Anchor = AnchorStyles.Left;
        this.lblIter.AutoSize = true;
        this.lblIter.Text = "Iterazioni:";

        // numIter
        this.numIter.Anchor = AnchorStyles.Left;
        this.numIter.Width = 70;
        this.numIter.Minimum = 50;
        this.numIter.Maximum = 50000;
        this.numIter.Increment = 50;
        this.numIter.Value = 256;
        this.numIter.ValueChanged += new EventHandler(this.NumIter_ValueChanged);

        // chkIterAuto
        this.chkIterAuto.Anchor = AnchorStyles.Left;
        this.chkIterAuto.AutoSize = true;
        this.chkIterAuto.Text = "Auto";
        this.chkIterAuto.CheckedChanged += new EventHandler(this.ChkIterAuto_CheckedChanged);

        // lblPalette
        this.lblPalette.Anchor = AnchorStyles.Left;
        this.lblPalette.AutoSize = true;
        this.lblPalette.Text = "Palette:";

        // cmbPalette
        this.cmbPalette.Anchor = AnchorStyles.Left;
        this.cmbPalette.Width = 110;
        this.cmbPalette.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbPalette.Items.AddRange(new object[] { "Fuoco", "Ghiaccio", "Termico", "Oceano", "Viola", "Deserto", "Foresta" });
        this.cmbPalette.SelectedIndex = 0;
        this.cmbPalette.SelectedIndexChanged += new EventHandler(this.CmbPalette_SelectedIndexChanged);

        // lblAA
        this.lblAA.Anchor = AnchorStyles.Left;
        this.lblAA.AutoSize = true;
        this.lblAA.Text = "AA:";

        // cmbAA
        this.cmbAA.Anchor = AnchorStyles.Left;
        this.cmbAA.Width = 60;
        this.cmbAA.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbAA.Items.AddRange(new object[] { "1x", "2x", "4x", "8x" });
        this.cmbAA.SelectedIndex = 0;
        this.cmbAA.SelectedIndexChanged += new EventHandler(this.CmbAA_SelectedIndexChanged);

        // enginePanel: gruppo radio motore di rendering
        this.enginePanel.Anchor = AnchorStyles.Left;
        this.enginePanel.AutoSize = true;
        this.enginePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.enginePanel.WrapContents = false;
        this.enginePanel.Margin = new Padding(0);
        this.enginePanel.Controls.Add(this.radioCpu);
        this.enginePanel.Controls.Add(this.radioCuda);
        this.enginePanel.Controls.Add(this.radioDx);

        // lblEngine
        this.lblEngine.Anchor = AnchorStyles.Left;
        this.lblEngine.AutoSize = true;
        this.lblEngine.Text = "Motore:";

        // lblGpu
        this.lblGpu.Anchor = AnchorStyles.Left;
        this.lblGpu.AutoSize = true;
        this.lblGpu.Text = "GPU:";
        this.lblGpu.Visible = false; // nascosto con motore CPU

        // cmbGpu (scheda video: "Auto" + schede enumerate di CUDA e DirectX)
        this.cmbGpu.Anchor = AnchorStyles.Left;
        this.cmbGpu.Width = 200;
        this.cmbGpu.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbGpu.Items.AddRange(new object[] { "Auto" });
        this.cmbGpu.SelectedIndex = 0;
        this.cmbGpu.Visible = false; // nascosto con motore CPU (visibile con CUDA/DirectX)
        this.cmbGpu.SelectedIndexChanged += new EventHandler(this.CmbGpu_SelectedIndexChanged);

        // lblPrec + precisionPanel: precisione CUDA (32 = float, 64 = double)
        this.lblPrec.Anchor = AnchorStyles.Left;
        this.lblPrec.AutoSize = true;
        this.lblPrec.Text = "Precisione:";

        this.precisionPanel.Anchor = AnchorStyles.Left;
        this.precisionPanel.AutoSize = true;
        this.precisionPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.precisionPanel.WrapContents = false;
        this.precisionPanel.Margin = new Padding(0);
        this.precisionPanel.Controls.Add(this.radPrec32);
        this.precisionPanel.Controls.Add(this.radPrec64);

        this.radPrec32.Anchor = AnchorStyles.Left;
        this.radPrec32.AutoSize = true;
        this.radPrec32.Text = "32";
        this.radPrec32.Enabled = false; // abilitata solo con motore CUDA
        this.radPrec32.CheckedChanged += new EventHandler(this.PrecRadio_CheckedChanged);

        this.radPrec64.Anchor = AnchorStyles.Left;
        this.radPrec64.AutoSize = true;
        this.radPrec64.Text = "64";
        this.radPrec64.Checked = true; // 64-bit (double) predefinita
        this.radPrec64.Enabled = false; // abilitata solo con motore CUDA
        this.radPrec64.CheckedChanged += new EventHandler(this.PrecRadio_CheckedChanged);

        // radioCpu
        this.radioCpu.Anchor = AnchorStyles.Left;
        this.radioCpu.AutoSize = true;
        this.radioCpu.Text = "CPU";
        this.radioCpu.Checked = true;
        this.radioCpu.CheckedChanged += new EventHandler(this.EngineRadio_CheckedChanged);

        // radioCuda
        this.radioCuda.Anchor = AnchorStyles.Left;
        this.radioCuda.AutoSize = true;
        this.radioCuda.Text = "CUDA";
        this.radioCuda.Enabled = false; // roadmap v1.4
        this.radioCuda.CheckedChanged += new EventHandler(this.EngineRadio_CheckedChanged);

        // radioDx
        this.radioDx.Anchor = AnchorStyles.Left;
        this.radioDx.AutoSize = true;
        this.radioDx.Text = "DirectX";
        this.radioDx.Enabled = false; // abilitato se DxMandelbrot.TryInitialize riesce
        this.radioDx.CheckedChanged += new EventHandler(this.EngineRadio_CheckedChanged);

        // statusStrip
        this.statusStrip.Items.AddRange(new ToolStripItem[] { this.lblStatus, this.statusHelp });

        // lblStatus
        this.lblStatus.Spring = true;
        this.lblStatus.Text = "Pronto";
        this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;

        // statusHelp
        this.statusHelp.Text = "Click sx/dx: zoom | Rotella: zoom | Trascina/frecce: sposta | R: reset | S: salva";
        this.statusHelp.ForeColor = System.Drawing.Color.Gray;
        this.statusHelp.Font = new System.Drawing.Font("Segoe UI", 8f);

        // pictureBox
        this.pictureBox.Dock = DockStyle.Fill;
        this.pictureBox.BackColor = System.Drawing.Color.Black;
        this.pictureBox.Cursor = Cursors.Cross;
        this.pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        this.pictureBox.MouseDown += new MouseEventHandler(this.PictureBox_MouseDown);
        this.pictureBox.MouseMove += new MouseEventHandler(this.PictureBox_MouseMove);
        this.pictureBox.MouseUp += new MouseEventHandler(this.PictureBox_MouseUp);
        this.pictureBox.MouseWheel += new MouseEventHandler(this.PictureBox_MouseWheel);
        this.pictureBox.MouseEnter += new EventHandler(this.PictureBox_MouseEnter);
        this.pictureBox.Resize += new EventHandler(this.PictureBox_Resize);

        // dxPanel: superficie DirectX (stessi handler mouse della pictureBox)
        this.dxPanel.Dock = DockStyle.Fill;
        this.dxPanel.BackColor = System.Drawing.Color.Black;
        this.dxPanel.Cursor = Cursors.Cross;
        this.dxPanel.Visible = false;
        this.dxPanel.MouseDown += new MouseEventHandler(this.PictureBox_MouseDown);
        this.dxPanel.MouseMove += new MouseEventHandler(this.PictureBox_MouseMove);
        this.dxPanel.MouseUp += new MouseEventHandler(this.PictureBox_MouseUp);
        this.dxPanel.MouseWheel += new MouseEventHandler(this.PictureBox_MouseWheel);
        this.dxPanel.MouseEnter += new EventHandler(this.PictureBox_MouseEnter);
        this.dxPanel.Resize += new EventHandler((s, e) => { _dxDirty = true; });

        // menuStrip
        this.menuStrip.Items.AddRange(new ToolStripItem[] { this.fileMenu, this.generaMenu, this.helpMenu });

        // fileMenu
        this.fileMenu.Text = "&File";
        this.fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
            this.loadZoneItem, this.saveZoneItem, this.fileSeparator,
            this.saveImageItem, this.fileSeparator2, this.benchmarkItem,
            this.fileSeparator3, this.exitItem });

        // loadZoneItem
        this.loadZoneItem.Text = "Carica &zona...";
        this.loadZoneItem.ShortcutKeys = Keys.Control | Keys.O;
        this.loadZoneItem.Click += new EventHandler(this.LoadZoneItem_Click);

        // saveZoneItem
        this.saveZoneItem.Text = "&Salva zona...";
        this.saveZoneItem.ShortcutKeys = Keys.Control | Keys.S;
        this.saveZoneItem.Click += new EventHandler(this.SaveZoneItem_Click);

        // saveImageItem
        this.saveImageItem.Text = "Salva immagine con &nome...";
        this.saveImageItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
        this.saveImageItem.Click += new EventHandler(this.SaveImageItem_Click);

        // generaMenu
        this.generaMenu.Text = "&Genera";
        this.generaMenu.DropDownItems.AddRange(new ToolStripItem[] {
            this.exportItem, this.videoItem, this.realTimeItem });

        // exportItem
        this.exportItem.Text = "&Screenshot...";
        this.exportItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.E;
        this.exportItem.Click += new EventHandler(this.ExportShotItem_Click);

        // videoItem
        this.videoItem.Text = "Video &zoom...";
        this.videoItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.V;
        this.videoItem.Click += new EventHandler(this.VideoItem_Click);

        // realTimeItem
        this.realTimeItem.Text = "Torna all'insieme (&RealTime)";
        this.realTimeItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.R;
        this.realTimeItem.Click += new EventHandler(this.RealTimeItem_Click);

        // benchmarkItem
        this.benchmarkItem.Text = "Bench&mark...";
        this.benchmarkItem.ShortcutKeys = Keys.Control | Keys.B;
        this.benchmarkItem.Click += new EventHandler(this.BenchmarkItem_Click);

        // exitItem
        this.exitItem.Text = "&Esci";
        this.exitItem.ShortcutKeys = Keys.Alt | Keys.F4;
        this.exitItem.Click += new EventHandler(this.ExitItem_Click);

        // helpMenu
        this.helpMenu.Text = "&Aiuto";
        this.helpMenu.DropDownItems.AddRange(new ToolStripItem[] { this.aboutItem, this.helpSeparator, this.logItem });

        // aboutItem
        this.aboutItem.Text = "&Informazioni...";
        this.aboutItem.ShortcutKeys = Keys.F1;
        this.aboutItem.Click += new EventHandler(this.AboutItem_Click);

        // logItem
        this.logItem.Text = "Mostra &log / diagnostica...";
        this.logItem.Click += new EventHandler(this.LogItem_Click);

        // toolTip: spiegazione di ogni controllo (si mostra al passaggio del mouse)
        this.toolTip.SetToolTip(this.btnReset, "Ripristina la vista iniziale dell'insieme (tasto R)");
        this.toolTip.SetToolTip(this.btnTorna, "Torna all'insieme con animazione RealTime (Ctrl+Shift+R)");
        this.toolTip.SetToolTip(this.btnBenchmark, "Apri il benchmark standard (Ctrl+B)");
        this.toolTip.SetToolTip(this.numIter, "Numero massimo di iterazioni per pixel (tasti +/-)");
        this.toolTip.SetToolTip(this.chkIterAuto, "Iterazioni automatiche: crescono con l'ingrandimento");
        this.toolTip.SetToolTip(this.cmbPalette, "Palette colori del frattale (Fuoco, Ghiaccio, Termico, Oceano, Viola, Deserto, Foresta)");
        this.toolTip.SetToolTip(this.cmbAA, "Antialiasing: 1x disattivato, 2x/4x/8x media dei pixel vicini");
        this.toolTip.SetToolTip(this.cmbGpu, "Scheda video da usare (Auto = la più potente)");
        this.toolTip.SetToolTip(this.radPrec32, "Precisione CUDA 32-bit (float): più veloce, meno precisa. Ignorata con CPU/DirectX.");
        this.toolTip.SetToolTip(this.radPrec64, "Precisione CUDA 64-bit (double): più precisa, più lenta. Ignorata con CPU/DirectX.");
        this.toolTip.SetToolTip(this.radioCpu, "Motore CPU multicore (sempre disponibile)");
        this.toolTip.SetToolTip(this.radioCuda, "Motore CUDA: GPU NVIDIA via ILGPU (float/double)");
        this.toolTip.SetToolTip(this.radioDx, "Motore DirectX: GPU in tempo reale (float)");
        // le voci menu sono ToolStripItem: si usa la proprietà ToolTipText
        this.loadZoneItem.ToolTipText = "Ricarica la vista salvata in un file JSON (Ctrl+O)";
        this.saveZoneItem.ToolTipText = "Salva la vista corrente (centro, scala, iterazioni) in un file JSON (Ctrl+S)";
        this.saveImageItem.ToolTipText = "Salva l'immagine corrente come PNG (Ctrl+Shift+S)";
        this.generaMenu.ToolTipText = "Genera output dalla vista: screenshot PNG o video zoom MP4";
        this.exportItem.ToolTipText = "Screenshot alla risoluzione vista con AA a scelta (Ctrl+Shift+E)";
        this.videoItem.ToolTipText = "Video MP4 dello zoom con AA a scelta (Ctrl+Shift+V, serve ffmpeg)";
        this.realTimeItem.ToolTipText = "Torna all'insieme con animazione RealTime (Esc per fermare, Ctrl+Shift+R)";
        this.benchmarkItem.ToolTipText = "Benchmark standard: 8 s ad alte iterazioni (Ctrl+B)";
        this.aboutItem.ToolTipText = "Informazioni su MandelC# (F1)";

        // MandelbrotForm
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1152, 720);
        this.Controls.Add(this.pictureBox);
        this.Controls.Add(this.dxPanel);
        this.Controls.Add(this.topPanel);
        this.Controls.Add(this.menuStrip);
        this.Controls.Add(this.statusStrip);
        this.MainMenuStrip = this.menuStrip;
        this.Text = "Visualizzatore Insieme di Mandelbrot";
        this.KeyPreview = true;
        this.KeyDown += new KeyEventHandler(this.MandelbrotForm_KeyDown);

        ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numIter)).EndInit();
        this.enginePanel.ResumeLayout(false);
        this.precisionPanel.ResumeLayout(false);
        this.row0.ResumeLayout(false);
        this.row1.ResumeLayout(false);
        this.topPanel.ResumeLayout(false);
        this.statusStrip.ResumeLayout(false);
        this.menuStrip.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
