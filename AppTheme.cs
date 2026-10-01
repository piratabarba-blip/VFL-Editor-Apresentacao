using System.Drawing.Drawing2D;

namespace VFL.VideoApresentacao;

internal static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(14, 17, 23);
    public static readonly Color Header = Color.FromArgb(21, 25, 34);
    public static readonly Color Surface = Color.FromArgb(26, 32, 44);
    public static readonly Color Card = Color.FromArgb(34, 41, 56);
    public static readonly Color Field = Color.FromArgb(34, 41, 56);
    public static readonly Color Border = Color.FromArgb(45, 55, 72);
    public static readonly Color Text = Color.FromArgb(248, 250, 252);
    public static readonly Color Muted = Color.FromArgb(148, 163, 184);
    public static readonly Color Dim = Color.FromArgb(100, 116, 139);
    public static readonly Color Primary = Color.FromArgb(237, 46, 71);
    public static readonly Color AccentCyan = Color.FromArgb(6, 182, 212);
    public static readonly Color Success = Color.FromArgb(16, 185, 129);
    public static readonly Color Button = Color.FromArgb(34, 41, 56);

    public static void Apply(Control root)
    {
        root.BackColor = (root.Tag as string) switch
        {
            "background" => Background,
            "header" => Header,
            "surface" or "sidebar" => Surface,
            "card" => Card,
            "rounded-header" => Header,
            "rounded-surface" => Surface,
            "accent" => Primary,
            _ => root is Form ? Background : root is Panel ? Surface : Background
        };
        root.ForeColor = Text;

        switch (root)
        {
            case TextBox textBox:
                textBox.BackColor = Field;
                textBox.ForeColor = Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox comboBox:
                comboBox.BackColor = Field;
                comboBox.ForeColor = Text;
                comboBox.FlatStyle = FlatStyle.Flat;
                break;
            case NumericUpDown numeric:
                numeric.BackColor = Field;
                numeric.ForeColor = Text;
                numeric.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListView list:
                list.BackColor = Field;
                list.ForeColor = Text;
                list.BorderStyle = BorderStyle.FixedSingle;
                break;
            case Button button when button.Tag as string != "primary":
                button.BackColor = Button;
                button.ForeColor = Text;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.BorderSize = 1;
                break;
            case Button button:
                button.BackColor = Primary;
                button.ForeColor = Text;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                break;
            case Label label:
                if (label.Tag as string != "accent") label.BackColor = Color.Transparent;
                if (label.Tag as string == "brand-badge") label.ForeColor = Primary;
                if (label.ForeColor == SystemColors.ControlText || label.ForeColor == Color.Black)
                    label.ForeColor = Text;
                break;
        }

        if (root is Button) Round(root, 10);
        else if (root is TextBox or ComboBox or NumericUpDown) Round(root, 6);
        else if (root is ListView) Round(root, 8);
        else if (root.Tag as string is "rounded-header" or "rounded-surface") Round(root, 14);
        else if (root.Tag as string == "card") Round(root, 12);

        foreach (Control child in root.Controls) Apply(child);
    }

    public static void ApplyWindowIcon(Form form)
    {
        try
        {
            using var extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (extracted is not null) form.Icon = (Icon)extracted.Clone();
        }
        catch { }
    }

    private static void Round(Control control, int radius)
    {
        void UpdateRegion()
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            var diameter = radius * 2;
            using var path = new GraphicsPath();
            var bounds = new Rectangle(0, 0, control.Width, control.Height);
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter - 1, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter - 1, bounds.Bottom - diameter - 1, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter - 1, diameter, diameter, 90, 90);
            path.CloseFigure();
            var previous = control.Region;
            control.Region = new Region(path);
            previous?.Dispose();
        }

        UpdateRegion();
        control.SizeChanged += (_, _) => UpdateRegion();
    }
}
