namespace VFL.VideoApresentacao;

internal sealed class ModernProgressBar : Control
{
    private int _value;

    public int Value
    {
        get => _value;
        set { _value = Math.Clamp(value, 0, 100); Invalidate(); }
    }

    public ModernProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        Height = 8;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(AppTheme.Field);
        var width = (int)(ClientSize.Width * (_value / 100d));
        if (width > 0)
        {
            using var brush = new SolidBrush(AppTheme.Primary);
            e.Graphics.FillRectangle(brush, 0, 0, width, ClientSize.Height);
        }
    }
}
