namespace MandelbrotViewer;

partial class Form1
{
    private System.ComponentModel.IContainer components = null!;
    private PictureBox pictureBox = null!;
    private Panel topPanel = null!;
    private Button btnReset = null!;
    private Button btnSave = null!;
    private Label lblIter = null!;
    private NumericUpDown numIter = null!;
    private Label lblStatus = null!;
    private Label lblHelp = null!;

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
        this.topPanel = new Panel();
        this.btnReset = new Button();
        this.btnSave = new Button();
        this.lblIter = new Label();
        this.numIter = new NumericUpDown();
        this.lblStatus = new Label();
        this.lblHelp = new Label();

        ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numIter)).BeginInit();
        this.topPanel.SuspendLayout();
        this.SuspendLayout();

        // topPanel
        this.topPanel.Dock = DockStyle.Top;
        this.topPanel.Height = 40;
        this.topPanel.Padding = new Padding(8, 6, 8, 6);
        this.topPanel.Controls.Add(this.lblHelp);
        this.topPanel.Controls.Add(this.lblStatus);
        this.topPanel.Controls.Add(this.numIter);
        this.topPanel.Controls.Add(this.lblIter);
        this.topPanel.Controls.Add(this.btnSave);
        this.topPanel.Controls.Add(this.btnReset);

        // btnReset
        this.btnReset.Dock = DockStyle.Left;
        this.btnReset.Width = 80;
        this.btnReset.Text = "Reset";
        this.btnReset.Click += new EventHandler(this.BtnReset_Click);

        // btnSave
        this.btnSave.Dock = DockStyle.Left;
        this.btnSave.Width = 100;
        this.btnSave.Text = "Salva PNG";
        this.btnSave.Click += new EventHandler(this.BtnSave_Click);

        // lblIter
        this.lblIter.Dock = DockStyle.Left;
        this.lblIter.Width = 70;
        this.lblIter.Text = "Iterazioni:";
        this.lblIter.TextAlign = ContentAlignment.MiddleRight;

        // numIter
        this.numIter.Dock = DockStyle.Left;
        this.numIter.Width = 80;
        this.numIter.Minimum = 50;
        this.numIter.Maximum = 5000;
        this.numIter.Increment = 50;
        this.numIter.Value = 256;
        this.numIter.ValueChanged += new EventHandler(this.NumIter_ValueChanged);

        // lblStatus
        this.lblStatus.Dock = DockStyle.Left;
        this.lblStatus.AutoSize = false;
        this.lblStatus.Width = 340;
        this.lblStatus.Text = "Pronto";
        this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        this.lblStatus.Padding = new Padding(12, 0, 0, 0);

        // lblHelp
        this.lblHelp.Dock = DockStyle.Fill;
        this.lblHelp.Text = "Trascina: zoom | Rotella: zoom | Tasto dx: allontana | R: reset | S: salva";
        this.lblHelp.TextAlign = ContentAlignment.MiddleRight;
        this.lblHelp.ForeColor = System.Drawing.Color.Gray;
        this.lblHelp.Font = new System.Drawing.Font("Segoe UI", 8f);

        // pictureBox
        this.pictureBox.Dock = DockStyle.Fill;
        this.pictureBox.BackColor = System.Drawing.Color.Black;
        this.pictureBox.Cursor = Cursors.Cross;
        this.pictureBox.SizeMode = PictureBoxSizeMode.Normal;
        this.pictureBox.MouseDown += new MouseEventHandler(this.PictureBox_MouseDown);
        this.pictureBox.MouseMove += new MouseEventHandler(this.PictureBox_MouseMove);
        this.pictureBox.MouseUp += new MouseEventHandler(this.PictureBox_MouseUp);
        this.pictureBox.MouseWheel += new MouseEventHandler(this.PictureBox_MouseWheel);
        this.pictureBox.Paint += new PaintEventHandler(this.PictureBox_Paint);
        this.pictureBox.Resize += new EventHandler(this.PictureBox_Resize);

        // Form1
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(900, 650);
        this.Controls.Add(this.pictureBox);
        this.Controls.Add(this.topPanel);
        this.Text = "Visualizzatore Insieme di Mandelbrot";
        this.KeyPreview = true;
        this.KeyDown += new KeyEventHandler(this.Form1_KeyDown);

        ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numIter)).EndInit();
        this.topPanel.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
