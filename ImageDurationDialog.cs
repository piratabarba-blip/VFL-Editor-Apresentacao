namespace VFL.VideoApresentacao;

internal sealed class ImageDurationDialog : Form
{
    private readonly NumericUpDown _seconds = new()
    {
        Minimum = 0.5m, Maximum = 600, DecimalPlaces = 1, Increment = 0.5m,
        Width = 150, TextAlign = HorizontalAlignment.Center
    };

    public double Seconds => (double)_seconds.Value;

    public ImageDurationDialog(string fileName, double initialSeconds)
    {
        Text = "Duração da imagem";
        ClientSize = new Size(500, 245);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Background;
        AppTheme.ApplyWindowIcon(this);
        _seconds.Value = (decimal)Math.Clamp(initialSeconds, 0.5, 600);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(24), Tag = "background" };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = "DURAÇÃO DA IMAGEM", Dock = DockStyle.Fill, ForeColor = AppTheme.Primary, Font = new Font("Segoe UI Semibold", 11), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        root.Controls.Add(new Label { Text = fileName, Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        var duration = new FlowLayoutPanel { Dock = DockStyle.Fill, Tag = "background", WrapContents = false };
        duration.Controls.Add(_seconds);
        duration.Controls.Add(new Label { Text = "segundos", AutoSize = true, Margin = new Padding(10, 7, 0, 0), ForeColor = AppTheme.Muted });
        root.Controls.Add(duration, 0, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Tag = "background", WrapContents = false };
        var ok = new Button { Text = "CONFIRMAR", Width = 140, Height = 40, DialogResult = DialogResult.OK, Tag = "primary" };
        var cancel = new Button { Text = "CANCELAR", Width = 110, Height = 40, DialogResult = DialogResult.Cancel };
        actions.Controls.Add(ok); actions.Controls.Add(cancel); root.Controls.Add(actions, 0, 3);
        Controls.Add(root);
        AcceptButton = ok; CancelButton = cancel;
        AppTheme.Apply(this);
    }
}
