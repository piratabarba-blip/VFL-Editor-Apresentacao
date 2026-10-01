namespace VFL.VideoApresentacao;

internal sealed class AppDialog : Form
{
    private AppDialog(string title, string message, string acceptText, string cancelText)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(540, 230);
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Background;
        AppTheme.ApplyWindowIcon(this);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(24), Tag = "background" };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        var heading = new Label { Text = title.ToUpperInvariant(), Dock = DockStyle.Fill, ForeColor = AppTheme.Primary, Font = new Font("Segoe UI Semibold", 12), TextAlign = ContentAlignment.MiddleLeft };
        var body = new Label { Text = message, Dock = DockStyle.Fill, ForeColor = AppTheme.Text, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 4, 0, 8) };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Tag = "background" };
        var accept = new Button { Text = acceptText.ToUpperInvariant(), Width = 150, Height = 42, DialogResult = DialogResult.Yes, Tag = "primary" };
        var cancel = new Button { Text = cancelText.ToUpperInvariant(), Width = 120, Height = 42, DialogResult = DialogResult.No };
        actions.Controls.Add(accept); actions.Controls.Add(cancel);
        root.Controls.Add(heading, 0, 0); root.Controls.Add(body, 0, 1); root.Controls.Add(actions, 0, 2);
        Controls.Add(root);
        AcceptButton = accept;
        CancelButton = cancel;
        AppTheme.Apply(this);
    }

    public static bool Confirm(IWin32Window owner, string title, string message, string acceptText = "Sim", string cancelText = "Não")
    {
        using var dialog = new AppDialog(title, message, acceptText, cancelText);
        return dialog.ShowDialog(owner) == DialogResult.Yes;
    }
}
