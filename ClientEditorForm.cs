namespace VFL.VideoApresentacao;

public sealed class ClientEditorForm : Form
{
    private readonly ClientProfile _profile;
    private readonly TextBox _name = new();
    private readonly TextBox _intro = new();
    private readonly TextBox _outro = new();
    private readonly TextBox _logo = new();
    private readonly TextBox _music = new();
    private readonly NumericUpDown _introSeconds = Number(1, 60, 4, 1);
    private readonly NumericUpDown _outroSeconds = Number(1, 60, 4, 1);
    private readonly NumericUpDown _fade = Number(.1m, 5, 1, .1m, 1);
    private readonly NumericUpDown _musicVolume = Number(0, 100, 18, 1);
    private readonly NumericUpDown _originalVolume = Number(0, 200, 100, 1);
    private readonly NumericUpDown _logoWidth = Number(30, 1000, 220, 10);
    private readonly NumericUpDown _logoLeft = Number(0, 500, 30, 5);
    private readonly NumericUpDown _logoBottom = Number(0, 500, 30, 5);

    public ClientEditorForm(ClientProfile profile)
    {
        _profile = profile;
        Text = profile.Name == "Novo cliente" ? "Novo cliente" : "Editar cliente";
        Size = new Size(940, 760); MinimumSize = new Size(820, 650);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 9.5f);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Background;
        AppTheme.ApplyWindowIcon(this);
        BuildUi();
        AppTheme.Apply(this);
        LoadValues();
        Shown += (_, _) => FitToWorkingArea();
    }

    private void BuildUi()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(24, 18, 24, 18), Tag = "background" };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        Controls.Add(shell);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = new Padding(0, 0, 0, 10), Tag = "rounded-header" };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 7)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); header.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        var accent = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Primary, Tag = "accent", Margin = new Padding(0) };
        header.Controls.Add(accent, 0, 0); header.SetRowSpan(accent, 2);
        header.Controls.Add(new Label { Text = "PERFIL DO CLIENTE", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 18), TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(14, 0, 0, 0) }, 1, 0);
        header.Controls.Add(new Label { Text = "Defina uma vez e reutilize em todos os próximos vídeos.", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.TopLeft, Padding = new Padding(14, 0, 0, 0) }, 1, 1);
        shell.Controls.Add(header, 0, 0);

        var bodyHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(18, 8, 18, 8), Tag = "rounded-surface", Margin = new Padding(0, 0, 0, 10) };
        shell.Controls.Add(bodyHost, 0, 1);
        var body = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, RowCount = 13, Tag = "surface", Padding = new Padding(2) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        for (var row = 1; row <= 4; row++) body.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        for (var row = 7; row <= 11; row++) body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        bodyHost.Controls.Add(body);

        AddText(body, 0, "Nome do cliente", _name, null);
        AddText(body, 1, "Abertura (imagem ou vídeo)", _intro, "Mídia|*.mp4;*.mov;*.mkv;*.avi;*.png;*.jpg;*.jpeg;*.webp|Todos|*.*");
        AddText(body, 2, "Encerramento (imagem ou vídeo)", _outro, "Mídia|*.mp4;*.mov;*.mkv;*.avi;*.png;*.jpg;*.jpeg;*.webp|Todos|*.*");
        AddText(body, 3, "Logo PNG", _logo, "Imagem PNG|*.png|Imagens|*.png;*.webp;*.jpg");
        AddText(body, 4, "Música (opcional)", _music, "Áudio|*.mp3;*.wav;*.m4a;*.aac;*.flac|Todos|*.*");
        var durationNote = new Label { Text = "Em arquivos de vídeo, a duração completa é usada automaticamente. Os tempos abaixo valem somente para imagens.", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.5f) };
        body.Controls.Add(durationNote, 0, 5); body.SetColumnSpan(durationNote, 3);
        body.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Border, Margin = new Padding(0, 7, 0, 7) }, 0, 6); body.SetColumnSpan(body.GetControlFromPosition(0, 6)!, 3);
        AddNumber(body, 7, "Duração da imagem inicial", _introSeconds, "segundos");
        AddNumber(body, 8, "Duração da imagem final", _outroSeconds, "segundos");
        AddNumber(body, 9, "Fade in / fade out", _fade, "segundos");
        AddNumber(body, 10, "Volume da música", _musicVolume, "%");
        AddNumber(body, 11, "Volume do vídeo", _originalVolume, "%");

        var logoGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, Tag = "surface", Margin = new Padding(0, 7, 0, 3) };
        for (var column = 0; column < 3; column++) { logoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); logoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f)); }
        logoGrid.Controls.Add(new Label { Text = "Largura", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 8, 0) }, 0, 0);
        logoGrid.Controls.Add(_logoWidth, 1, 0);
        logoGrid.Controls.Add(new Label { Text = "Margem esquerda", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(16, 7, 8, 0) }, 2, 0);
        logoGrid.Controls.Add(_logoLeft, 3, 0);
        logoGrid.Controls.Add(new Label { Text = "Margem inferior", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(16, 7, 8, 0) }, 4, 0);
        logoGrid.Controls.Add(_logoBottom, 5, 0);
        foreach (var number in new[] { _logoWidth, _logoLeft, _logoBottom }) { number.Dock = DockStyle.Fill; number.MaximumSize = new Size(150, 0); }
        body.Controls.Add(new Label { Text = "Posição e tamanho do logo", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 12);
        body.Controls.Add(logoGrid, 1, 12); body.SetColumnSpan(logoGrid, 2);

        var footer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 10), Tag = "rounded-surface" };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Tag = "surface" };
        var save = new Button { Text = "SALVAR CLIENTE", Width = 168, Height = 42, Tag = "primary" };
        var cancel = new Button { Text = "CANCELAR", Width = 122, Height = 42, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => Save();
        buttons.Controls.Add(save); buttons.Controls.Add(cancel); footer.Controls.Add(buttons); shell.Controls.Add(footer, 0, 2);
        CancelButton = cancel;
    }

    private static void AddText(TableLayoutPanel root, int row, string label, TextBox box, string? filter)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = AppTheme.Muted }, 0, row);
        box.Dock = DockStyle.Fill; box.Margin = new Padding(0, 8, 10, 8); root.Controls.Add(box, 1, row);
        if (filter is null) { root.SetColumnSpan(box, 2); return; }
        var browse = new Button { Text = "PROCURAR", Dock = DockStyle.Fill, Margin = new Padding(0, 5, 0, 5) };
        browse.Click += (_, _) => { using var d = new OpenFileDialog { Filter = filter }; if (d.ShowDialog() == DialogResult.OK) box.Text = d.FileName; };
        root.Controls.Add(browse, 2, row);
    }

    private static void AddNumber(TableLayoutPanel root, int row, string label, NumericUpDown number, string suffix)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = AppTheme.Muted }, 0, row);
        number.Width = 130; number.Anchor = AnchorStyles.Left; root.Controls.Add(number, 1, row);
        root.Controls.Add(new Label { Text = suffix, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = AppTheme.Muted }, 2, row);
    }

    private void LoadValues()
    {
        _name.Text = _profile.Name; _intro.Text = _profile.IntroPath; _outro.Text = _profile.OutroPath;
        _logo.Text = _profile.LogoPath; _music.Text = _profile.MusicPath;
        _introSeconds.Value = (decimal)_profile.IntroImageSeconds; _outroSeconds.Value = (decimal)_profile.OutroImageSeconds;
        _fade.Value = (decimal)_profile.FadeSeconds; _musicVolume.Value = (decimal)_profile.MusicVolumePercent;
        _originalVolume.Value = (decimal)_profile.OriginalVolumePercent; _logoWidth.Value = _profile.LogoWidth;
        _logoLeft.Value = _profile.LogoMarginLeft; _logoBottom.Value = _profile.LogoMarginBottom;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("Informe o nome do cliente."); return; }
        _profile.Name = _name.Text.Trim(); _profile.IntroPath = _intro.Text.Trim(); _profile.OutroPath = _outro.Text.Trim();
        _profile.LogoPath = _logo.Text.Trim(); _profile.MusicPath = _music.Text.Trim();
        _profile.IntroImageSeconds = (double)_introSeconds.Value; _profile.OutroImageSeconds = (double)_outroSeconds.Value;
        _profile.FadeSeconds = (double)_fade.Value; _profile.MusicVolumePercent = (double)_musicVolume.Value;
        _profile.OriginalVolumePercent = (double)_originalVolume.Value; _profile.LogoWidth = (int)_logoWidth.Value;
        _profile.LogoMarginLeft = (int)_logoLeft.Value; _profile.LogoMarginBottom = (int)_logoBottom.Value;
        DialogResult = DialogResult.OK;
    }

    private static NumericUpDown Number(decimal min, decimal max, decimal value, decimal increment, int decimals = 0) =>
        new() { Minimum = min, Maximum = max, Value = value, Increment = increment, DecimalPlaces = decimals, ThousandsSeparator = true };

    private void FitToWorkingArea()
    {
        var area = Screen.FromControl(this).WorkingArea;
        Size = new Size(Math.Min(Width, area.Width - 24), Math.Min(Height, area.Height - 24));
        Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
    }
}
