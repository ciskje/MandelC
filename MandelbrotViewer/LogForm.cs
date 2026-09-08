namespace MandelbrotViewer;

// Dialog di sola lettura per il log/diagnostica, con pulsante per copiare negli appunti.
internal sealed class LogForm : Form
{
    public LogForm(string text)
    {
        Text = "Log / Diagnostica";
        Program.ApplyIcon(this);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        ClientSize = new Size(720, 480);
        MinimumSize = new Size(440, 320);

        var txt = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            HideSelection = true,
            Font = new Font("Consolas", 9.75f),
            Dock = DockStyle.Fill,
            Text = text,
        };

        var closeBtn = new Button { Text = "Chiudi", Dock = DockStyle.Right, Width = 90, DialogResult = DialogResult.OK };
        var copyBtn = new Button { Text = "Copia negli appunti", Dock = DockStyle.Right, Width = 165 };
        copyBtn.Click += (s, e) =>
        {
            Clipboard.SetText(txt.Text);
            copyBtn.Text = "Copiato!";
        };

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(6) };
        bottom.Controls.Add(copyBtn);
        bottom.Controls.Add(closeBtn);

        Controls.Add(txt);
        Controls.Add(bottom);

        Shown += (s, e) =>
        {
            txt.SelectionStart = 0;
            txt.SelectionLength = 0;
            closeBtn.Focus();
        };

        AcceptButton = closeBtn;
        CancelButton = closeBtn;
    }
}
