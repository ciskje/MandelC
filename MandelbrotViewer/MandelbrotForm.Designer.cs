namespace MandelbrotViewer;

    partial class MandelbrotForm
{
    private System.ComponentModel.IContainer components = null!;
    private PictureBox pictureBox = null!;
    private Panel dxPanel = null!;
    private Panel topPanel = null!;
    private TableLayoutPanel layoutTop = null!;
    private Button btnReset = null!;
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
    private ToolStripMenuItem exportItem = null!;
    private ToolStripMenuItem videoItem = null!;
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
        this.layoutTop = new TableLayoutPanel();
        this.btnReset = new Button();
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
        this.exportItem = new ToolStripMenuItem();
        this.videoItem = new ToolStripMenuItem();
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
        this.layoutTop.SuspendLayout();
        this.enginePanel.SuspendLayout();
        this.precisionPanel.SuspendLayout();
        this.statusStrip.SuspendLayout();
        this.menuStrip.SuspendLayout();
        this.SuspendLayout();

        // topPanel
        this.topPanel.Dock = DockStyle.Top;
        this.topPanel.Height = 40;
        this.topPanel.Padding = new Padding(8, 6, 8, 6);
        this.topPanel.Controls.Add(this.layoutTop);

        // layoutTop: riga unica, celle auto; i controlli con Anchor=None restano centrati
        // verticalmente (risolve anche l'allineamento numero/label iterazioni).
        this.layoutTop.Dock = DockStyle.Left;
        this.layoutTop.AutoSize = true;
        this.layoutTop.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.layoutTop.Margin = new Padding(0);
        this.layoutTop.ColumnCount = 15;
        this.layoutTop.RowCount = 1;
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        this.layoutTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this.layoutTop.Controls.Add(this.btnReset, 0, 0);
        this.layoutTop.Controls.Add(this.btnBenchmark, 1, 0);
        this.layoutTop.Controls.Add(this.lblIter, 2, 0);
        this.layoutTop.Controls.Add(this.numIter, 3, 0);
        this.layoutTop.Controls.Add(this.chkIterAuto, 4, 0);
        this.layoutTop.Controls.Add(this.lblPalette, 5, 0);
        this.layoutTop.Controls.Add(this.cmbPalette, 6, 0);
        this.layoutTop.Controls.Add(this.lblAA, 7, 0);
        this.layoutTop.Controls.Add(this.cmbAA, 8, 0);
        this.layoutTop.Controls.Add(this.lblEngine, 9, 0);
        this.layoutTop.Controls.Add(this.enginePanel, 10, 0);
        this.layoutTop.Controls.Add(this.lblGpu, 11, 0);
        this.layoutTop.Controls.Add(this.cmbGpu, 12, 0);
        this.layoutTop.Controls.Add(this.lblPrec, 13, 0);
        this.layoutTop.Controls.Add(this.precisionPanel, 14, 0);

        // btnReset
        this.btnReset.Anchor = AnchorStyles.None;
        this.btnReset.Size = new System.Drawing.Size(80, 23);
        this.btnReset.Text = "Reset";
        this.btnReset.Click += new EventHandler(this.BtnReset_Click);

        // btnBenchmark (il salvataggio PNG resta nel menu File)
        this.btnBenchmark.Anchor = AnchorStyles.None;
        this.btnBenchmark.Size = new System.Drawing.Size(100, 23);
        this.btnBenchmark.Text = "Benchmark";
        this.btnBenchmark.Click += new EventHandler(this.BenchmarkItem_Click);

        // lblIter
        this.lblIter.Anchor = AnchorStyles.None;
        this.lblIter.AutoSize = true;
        this.lblIter.Text = "Iterazioni:";

        // numIter
        this.numIter.Anchor = AnchorStyles.None;
        this.numIter.Width = 70;
        this.numIter.Minimum = 50;
        this.numIter.Maximum = 50000;
        this.numIter.Increment = 50;
        this.numIter.Value = 256;
        this.numIter.ValueChanged += new EventHandler(this.NumIter_ValueChanged);

        // chkIterAuto
        this.chkIterAuto.Anchor = AnchorStyles.None;
        this.chkIterAuto.AutoSize = true;
        this.chkIterAuto.Text = "Auto";
        this.chkIterAuto.CheckedChanged += new EventHandler(this.ChkIterAuto_CheckedChanged);

        // lblPalette
        this.lblPalette.Anchor = AnchorStyles.None;
        this.lblPalette.AutoSize = true;
        this.lblPalette.Text = "Palette:";

        // cmbPalette
        this.cmbPalette.Anchor = AnchorStyles.None;
        this.cmbPalette.Width = 110;
        this.cmbPalette.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbPalette.Items.AddRange(new object[] { "Fuoco", "Ghiaccio", "Termico", "Oceano", "Viola", "Deserto", "Foresta" });
        this.cmbPalette.SelectedIndex = 0;
        this.cmbPalette.SelectedIndexChanged += new EventHandler(this.CmbPalette_SelectedIndexChanged);

        // lblAA
        this.lblAA.Anchor = AnchorStyles.None;
        this.lblAA.AutoSize = true;
        this.lblAA.Text = "AA:";

        // cmbAA
        this.cmbAA.Anchor = AnchorStyles.None;
        this.cmbAA.Width = 60;
        this.cmbAA.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbAA.Items.AddRange(new object[] { "1x", "2x", "4x", "8x" });
        this.cmbAA.SelectedIndex = 0;
        this.cmbAA.SelectedIndexChanged += new EventHandler(this.CmbAA_SelectedIndexChanged);

        // enginePanel: gruppo radio motore di rendering
        this.enginePanel.Anchor = AnchorStyles.None;
        this.enginePanel.AutoSize = true;
        this.enginePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.enginePanel.WrapContents = false;
        this.enginePanel.Margin = new Padding(0);
        this.enginePanel.Controls.Add(this.radioCpu);
        this.enginePanel.Controls.Add(this.radioCuda);
        this.enginePanel.Controls.Add(this.radioDx);

        // lblEngine
        this.lblEngine.Anchor = AnchorStyles.None;
        this.lblEngine.AutoSize = true;
        this.lblEngine.Text = "Motore:";

        // lblGpu
        this.lblGpu.Anchor = AnchorStyles.None;
        this.lblGpu.AutoSize = true;
        this.lblGpu.Text = "GPU:";
        this.lblGpu.Visible = false; // nascosto con motore CPU

        // cmbGpu (scheda video: "Auto" + schede enumerate di CUDA e DirectX)
        this.cmbGpu.Anchor = AnchorStyles.None;
        this.cmbGpu.Width = 200;
        this.cmbGpu.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbGpu.Items.AddRange(new object[] { "Auto" });
        this.cmbGpu.SelectedIndex = 0;
        this.cmbGpu.Visible = false; // nascosto con motore CPU (visibile con CUDA/DirectX)
        this.cmbGpu.SelectedIndexChanged += new EventHandler(this.CmbGpu_SelectedIndexChanged);

        // lblPrec + precisionPanel: precisione CUDA (32 = float, 64 = double)
        this.lblPrec.Anchor = AnchorStyles.None;
        this.lblPrec.AutoSize = true;
        this.lblPrec.Text = "Precisione:";

        this.precisionPanel.Anchor = AnchorStyles.None;
        this.precisionPanel.AutoSize = true;
        this.precisionPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.precisionPanel.WrapContents = false;
        this.precisionPanel.Margin = new Padding(0);
        this.precisionPanel.Controls.Add(this.radPrec32);
        this.precisionPanel.Controls.Add(this.radPrec64);

        this.radPrec32.Anchor = AnchorStyles.None;
        this.radPrec32.AutoSize = true;
        this.radPrec32.Text = "32";
        this.radPrec32.Enabled = false; // abilitata solo con motore CUDA
        this.radPrec32.CheckedChanged += new EventHandler(this.PrecRadio_CheckedChanged);

        this.radPrec64.Anchor = AnchorStyles.None;
        this.radPrec64.AutoSize = true;
        this.radPrec64.Text = "64";
        this.radPrec64.Checked = true; // 64-bit (double) predefinita
        this.radPrec64.Enabled = false; // abilitata solo con motore CUDA
        this.radPrec64.CheckedChanged += new EventHandler(this.PrecRadio_CheckedChanged);

        // radioCpu
        this.radioCpu.Anchor = AnchorStyles.None;
        this.radioCpu.AutoSize = true;
        this.radioCpu.Text = "CPU";
        this.radioCpu.Checked = true;
        this.radioCpu.CheckedChanged += new EventHandler(this.EngineRadio_CheckedChanged);

        // radioCuda
        this.radioCuda.Anchor = AnchorStyles.None;
        this.radioCuda.AutoSize = true;
        this.radioCuda.Text = "CUDA";
        this.radioCuda.Enabled = false; // roadmap v1.4
        this.radioCuda.CheckedChanged += new EventHandler(this.EngineRadio_CheckedChanged);

        // radioDx
        this.radioDx.Anchor = AnchorStyles.None;
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
        this.pictureBox.SizeMode = PictureBoxSizeMode.Normal;
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

        // menuStrip
        this.menuStrip.Items.AddRange(new ToolStripItem[] { this.fileMenu, this.helpMenu });

        // fileMenu
        this.fileMenu.Text = "&File";
        this.fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
            this.loadZoneItem, this.saveZoneItem, this.fileSeparator,
            this.saveImageItem, this.exportItem, this.videoItem, this.fileSeparator2, this.benchmarkItem,
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

        // exportItem
        this.exportItem.Text = "Esporta PNG ad alta &risoluzione...";
        this.exportItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.E;
        this.exportItem.Click += new EventHandler(this.ExportItem_Click);

        // videoItem
        this.videoItem.Text = "Esporta &video zoom...";
        this.videoItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.V;
        this.videoItem.Click += new EventHandler(this.VideoItem_Click);

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
        this.exportItem.ToolTipText = "Rende la vista a risoluzione scelta e salva il PNG (Ctrl+Shift+E)";
        this.videoItem.ToolTipText = "Video MP4 dello zoom dall'insieme alla vista (Ctrl+Shift+V, serve ffmpeg)";
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
        this.layoutTop.ResumeLayout(false);
        this.topPanel.ResumeLayout(false);
        this.statusStrip.ResumeLayout(false);
        this.menuStrip.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
