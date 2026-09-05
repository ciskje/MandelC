namespace MandelbrotViewer;

partial class BenchmarkForm
{
    private System.ComponentModel.IContainer components = null!;
    private Label lblInfo = null!;
    private Label lblResult = null!;
    private Label lblDetail = null!;
    private Panel bottomPanel = null!;
    private ProgressBar progressBar = null!;
    private Label lblLive = null!;
    private Button btnStart = null!;
    private Button btnClose = null!;

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
        this.lblInfo = new Label();
        this.lblResult = new Label();
        this.lblDetail = new Label();
        this.bottomPanel = new Panel();
        this.progressBar = new ProgressBar();
        this.lblLive = new Label();
        this.btnStart = new Button();
        this.btnClose = new Button();

        this.bottomPanel.SuspendLayout();
        this.SuspendLayout();

        // lblInfo
        this.lblInfo.Dock = DockStyle.Top;
        this.lblInfo.Height = 56;
        this.lblInfo.Padding = new Padding(12, 10, 12, 0);
        this.lblInfo.ForeColor = System.Drawing.Color.DimGray;

        // lblResult: il numero del test, in grande
        this.lblResult.Dock = DockStyle.Fill;
        this.lblResult.Text = "—";
        this.lblResult.Font = new System.Drawing.Font("Segoe UI", 36f, System.Drawing.FontStyle.Bold);
        this.lblResult.TextAlign = ContentAlignment.MiddleCenter;

        // lblDetail
        this.lblDetail.Dock = DockStyle.Bottom;
        this.lblDetail.Height = 28;
        this.lblDetail.Text = "";
        this.lblDetail.TextAlign = ContentAlignment.MiddleCenter;
        this.lblDetail.ForeColor = System.Drawing.Color.DimGray;
        this.lblDetail.Font = new System.Drawing.Font("Segoe UI", 8f);

        // bottomPanel
        this.bottomPanel.Dock = DockStyle.Bottom;
        this.bottomPanel.Height = 92;
        this.bottomPanel.Padding = new Padding(12, 8, 12, 10);
        this.bottomPanel.Controls.Add(this.lblLive);
        this.bottomPanel.Controls.Add(this.btnStart);
        this.bottomPanel.Controls.Add(this.btnClose);
        this.bottomPanel.Controls.Add(this.progressBar);

        // progressBar
        this.progressBar.Dock = DockStyle.Top;
        this.progressBar.Height = 23;
        this.progressBar.Minimum = 0;
        this.progressBar.Maximum = 1000;

        // lblLive
        this.lblLive.Dock = DockStyle.Fill;
        this.lblLive.Text = "";
        this.lblLive.TextAlign = ContentAlignment.MiddleLeft;

        // btnStart
        this.btnStart.Dock = DockStyle.Right;
        this.btnStart.Width = 90;
        this.btnStart.Text = "Avvia";
        this.btnStart.Click += new EventHandler(this.BtnStart_Click);

        // btnClose
        this.btnClose.Dock = DockStyle.Right;
        this.btnClose.Width = 90;
        this.btnClose.Text = "Chiudi";
        this.btnClose.DialogResult = DialogResult.Cancel;
        this.btnClose.Click += new EventHandler(this.BtnClose_Click);

        // BenchmarkForm
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(520, 360);
        this.Controls.Add(this.lblResult);
        this.Controls.Add(this.lblDetail);
        this.Controls.Add(this.lblInfo);
        this.Controls.Add(this.bottomPanel);
        this.Text = "Benchmark";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.CancelButton = this.btnClose;

        this.bottomPanel.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
