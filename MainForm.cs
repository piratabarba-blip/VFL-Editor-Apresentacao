using System.Diagnostics;

namespace VFL.VideoApresentacao;

public sealed class MainForm : Form
{
    private readonly ProfileStore _store = new();
    private readonly MediaRenderer _renderer = new();
    private readonly List<ClientProfile> _profiles;
    private readonly ComboBox _clients = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly MediaSequenceControl _sequence = new();
    private readonly TextBox _output = new();
    private readonly Button _render = new() { Text = "GERAR VÍDEO", Height = 46 };
    private readonly Button _cancel = new() { Text = "Cancelar", Enabled = false };
    private readonly Button _addClient = new() { Text = "+ NOVO CLIENTE", Tag = "primary" };
    private readonly Button _editClient = new() { Text = "EDITAR CLIENTE" };
    private readonly Button _deleteClient = new() { Text = "EXCLUIR CLIENTE" };
    private readonly Button _selectOutput = new() { Text = "ESCOLHER DESTINO" };
    private readonly Label _outputFolder = new() { Text = "Definido automaticamente ao adicionar mídia", AutoEllipsis = true, ForeColor = AppTheme.Muted };
    private readonly ModernProgressBar _progress = new() { Height = 8 };
    private readonly Label _status = new() { Text = "Pronto para começar.", AutoSize = true };
    private CancellationTokenSource? _cts;

    public MainForm()
    {
        _profiles = _store.Load();
        Text = "VFL — Editor de Vídeo de Apresentação";
        MinimumSize = new Size(1180, 720);
        Size = new Size(1440, 860);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Background;
        AppTheme.ApplyWindowIcon(this);
        BuildProfessionalUi();
        AppTheme.Apply(this);
        RefreshClients();
        _sequence.SequenceChanged += (_, _) => SuggestOutputFromSequence();
        Shown += (_, _) => FitToWorkingArea();
    }

    private void BuildProfessionalUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Tag = "background", Margin = Padding.Empty };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); Controls.Add(root);

        var topbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(16, 10, 16, 9), Tag = "header", Margin = Padding.Empty };
        topbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360)); topbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); topbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        topbar.Controls.Add(CreateBrand("VFL Editor de Apresentação", "EDIÇÃO DE APRESENTAÇÃO  •  v1.2"), 0, 0);
        topbar.Controls.Add(new Label { Text = "Monte vídeos padronizados por cliente, com sequência livre, marca, música e transições.", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.8f), Padding = new Padding(16, 0, 0, 0) }, 1, 0);
        topbar.Controls.Add(new Label { Text = "SUÍTE VFL  •  PROCESSAMENTO LOCAL", Dock = DockStyle.Fill, ForeColor = AppTheme.AccentCyan, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 7.5f) }, 2, 0); root.Controls.Add(topbar, 0, 0);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Tag = "background", Margin = Padding.Empty };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290)); main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310)); root.Controls.Add(main, 0, 1);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, Padding = new Padding(16, 14, 16, 14), Tag = "sidebar", Margin = Padding.Empty };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); left.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); left.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); left.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); left.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.Controls.Add(SectionTitle("PERFIL DO CLIENTE", "◆"), 0, 0);
        left.Controls.Add(new Label { Text = "CLIENTE ATIVO", Dock = DockStyle.Fill, ForeColor = AppTheme.Dim, Font = new Font("Segoe UI Semibold", 7.5f), TextAlign = ContentAlignment.BottomLeft }, 0, 1);
        _clients.Dock = DockStyle.Fill; _clients.Margin = new Padding(0, 5, 0, 5); left.Controls.Add(_clients, 0, 2);
        foreach (var item in new[] { (_addClient, 3), (_editClient, 4), (_deleteClient, 5) }) { item.Item1.Dock = DockStyle.Fill; item.Item1.Margin = new Padding(0, 6, 0, 0); left.Controls.Add(item.Item1, 0, item.Item2); }
        left.Controls.Add(new Label { Text = "O perfil guarda abertura, encerramento, logotipo, música, volumes e transições. Configure uma vez e reutilize nos próximos trabalhos.", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, Font = new Font("Segoe UI", 8.4f), Padding = new Padding(10, 14, 10, 10), Tag = "card" }, 0, 6); main.Controls.Add(left, 0, 0);

        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Tag = "background", Margin = Padding.Empty };
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var workspaceHeader = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(20, 10, 20, 6), Tag = "background" };
        workspaceHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); workspaceHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        workspaceHeader.Controls.Add(new Label { Text = "SEQUÊNCIA DA APRESENTAÇÃO", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 10), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        workspaceHeader.Controls.Add(new Label { Text = "VÍDEOS E IMAGENS EM ORDEM LIVRE", Dock = DockStyle.Fill, ForeColor = AppTheme.AccentCyan, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 8) }, 1, 0);
        workspace.Controls.Add(workspaceHeader, 0, 0); _sequence.Margin = new Padding(18, 0, 18, 18); workspace.Controls.Add(_sequence, 0, 1); main.Controls.Add(workspace, 1, 0);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, Padding = new Padding(16, 14, 16, 14), Tag = "sidebar", Margin = Padding.Empty };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 150)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        right.Controls.Add(SectionTitle("EXPORTAÇÃO", "↗"), 0, 0);
        var outputCard = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Tag = "card", Padding = new Padding(12), Margin = new Padding(0, 0, 0, 8) };
        outputCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); outputCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); outputCard.Controls.Add(new Label { Text = "ARQUIVO DE SAÍDA", Dock = DockStyle.Fill, ForeColor = AppTheme.Dim, Font = new Font("Segoe UI Semibold", 7.5f) }, 0, 0);
        _outputFolder.Dock = DockStyle.Fill; _outputFolder.Font = new Font("Consolas", 8); outputCard.Controls.Add(_outputFolder, 0, 1); right.Controls.Add(outputCard, 0, 1);
        _selectOutput.Dock = DockStyle.Fill; _selectOutput.Margin = new Padding(0, 4, 0, 4); right.Controls.Add(_selectOutput, 0, 2);
        right.Controls.Add(SectionTitle("FORMATO FINAL", "▣"), 0, 3);
        var specs = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, Tag = "card", Padding = new Padding(12), Margin = new Padding(0, 6, 0, 8) };
        foreach (var text in new[] { "MP4 H.264 de alta qualidade", "Áudio AAC", "Intro e encerramento automáticos", "Marca e música do cliente", "Original sempre preservado" }) specs.Controls.Add(new Label { Text = "•  " + text, Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, Font = new Font("Segoe UI", 8.3f), TextAlign = ContentAlignment.MiddleLeft });
        right.Controls.Add(specs, 0, 4); right.Controls.Add(new Label { Text = "Adicione quantos vídeos e imagens quiser, reorganize a sequência e gere o resultado final em uma única etapa.", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, Font = new Font("Segoe UI", 8.4f), Padding = new Padding(4, 12, 4, 4) }, 0, 5);
        StylePrimary(_render); _render.Text = "GERAR VÍDEO"; _render.Dock = DockStyle.Fill; _render.Margin = new Padding(0, 6, 0, 0); right.Controls.Add(_render, 0, 6); main.Controls.Add(right, 2, 0);

        var statusbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(16, 4, 16, 5), Tag = "header", Margin = Padding.Empty };
        statusbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); statusbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); statusbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); statusbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        statusbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); statusbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 5)); statusbar.Controls.Add(new Label { Text = "●  PROCESSAMENTO LOCAL", Dock = DockStyle.Fill, ForeColor = AppTheme.AccentCyan, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 7.5f) }, 0, 0);
        _status.Dock = DockStyle.Fill; _status.ForeColor = AppTheme.Muted; _status.TextAlign = ContentAlignment.MiddleLeft; statusbar.Controls.Add(_status, 1, 0);
        _cancel.Dock = DockStyle.Fill; _cancel.Margin = new Padding(0, 2, 10, 2); statusbar.Controls.Add(_cancel, 2, 0); statusbar.Controls.Add(new Label { Text = "ORIGINAL PRESERVADO", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 8) }, 3, 0);
        _progress.Dock = DockStyle.Fill; statusbar.Controls.Add(_progress, 0, 1); statusbar.SetColumnSpan(_progress, 4); root.Controls.Add(statusbar, 0, 2);

        _addClient.Click += (_, _) => EditClient(null);
        _editClient.Click += (_, _) => { if (SelectedProfile is { } profile) EditClient(profile); else MessageBox.Show("Crie um cliente primeiro."); };
        _deleteClient.Click += (_, _) => DeleteSelectedClient();
        _selectOutput.Click += (_, _) => BrowseForFile(_output, "Vídeo MP4|*.mp4", true);
        _render.Click += async (_, _) => await RenderAsync(); _cancel.Click += (_, _) => _cts?.Cancel();
        _output.TextChanged += (_, _) => _outputFolder.Text = string.IsNullOrWhiteSpace(_output.Text) ? "Definido automaticamente ao adicionar mídia" : _output.Text;
    }

    private Control CreateBrand(string title, string badge)
    {
        var brand = new Panel { Dock = DockStyle.Fill, Tag = "header", Margin = Padding.Empty };
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "vfl-suite-logo.png");
        Control mark = File.Exists(logoPath)
            ? new PictureBox { Image = Image.FromFile(logoPath), SizeMode = PictureBoxSizeMode.Zoom, Location = new Point(0, 5), Size = new Size(44, 44) }
            : new Label { Text = "V", Location = new Point(0, 5), Size = new Size(44, 44), BackColor = AppTheme.Primary, Tag = "accent", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Black", 15) };
        brand.Controls.Add(mark);
        brand.Controls.Add(new Label { Text = title, Location = new Point(50, 3), Size = new Size(300, 25), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI Semibold", 11.5f), Margin = Padding.Empty });
        brand.Controls.Add(new Label { Text = badge, Location = new Point(50, 29), Size = new Size(300, 16), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, Tag = "brand-badge", TextAlign = ContentAlignment.TopLeft, Font = new Font("Segoe UI Semibold", 6.8f), Margin = Padding.Empty });
        return brand;
    }

    private static Label SectionTitle(string text, string icon) => new() { Text = $"{icon}  {text}", Dock = DockStyle.Fill, ForeColor = AppTheme.Text, Font = new Font("Segoe UI Semibold", 9), TextAlign = ContentAlignment.MiddleLeft };

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Tag = "background" };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(28, 20, 28, 16), Tag = "header" };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        var brand = new Label
        {
            Text = "VFL", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Tag = "accent",
            BackColor = AppTheme.Primary, ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI Black", 16)
        };
        header.Controls.Add(brand, 0, 0);
        var accent = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Primary, Margin = new Padding(0, 4, 12, 4), Tag = "accent" };
        header.Controls.Add(accent, 1, 0);
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Tag = "header" };
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        heading.Controls.Add(new Label { Text = "VFL Editor de Apresentação", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 23), TextAlign = ContentAlignment.BottomLeft }, 0, 0);
        heading.Controls.Add(new Label { Text = "Monte vídeos padronizados por cliente, com marca, música e transições.", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.TopLeft }, 0, 1);
        header.Controls.Add(heading, 2, 0);
        header.Controls.Add(new Label { Text = "PROCESSAMENTO LOCAL  |  v1.1", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 8.5f) }, 3, 0);
        root.Controls.Add(header, 0, 0);

        var contentHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 22, 28, 10), Tag = "background" };
        root.Controls.Add(contentHost, 0, 1);
        var work = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26, 18, 26, 18), RowCount = 9, ColumnCount = 1, Tag = "rounded-surface" };
        work.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        work.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        work.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        work.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        work.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        work.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        work.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        work.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        work.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contentHost.Controls.Add(work);
        work.Controls.Add(new Label { Text = "NOVO PROCESSAMENTO", Dock = DockStyle.Fill, ForeColor = AppTheme.Primary, Font = new Font("Segoe UI Semibold", 9) }, 0, 0);

        var clientLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Tag = "surface" };
        clientLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        clientLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        clientLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        clientLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        clientLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        clientLayout.Controls.Add(new Label { Text = "CLIENTE", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _clients.Dock = DockStyle.Fill; _clients.Margin = new Padding(0, 8, 12, 8);
        clientLayout.Controls.Add(_clients, 1, 0);
        var add = new Button { Text = "+ NOVO CLIENTE", Dock = DockStyle.Fill, Margin = new Padding(0, 6, 8, 6) };
        var edit = new Button { Text = "EDITAR", Dock = DockStyle.Fill, Margin = new Padding(0, 6, 8, 6) };
        var delete = new Button { Text = "EXCLUIR", Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 6) };
        add.Click += (_, _) => EditClient(null);
        edit.Click += (_, _) => { if (SelectedProfile is { } p) EditClient(p); else MessageBox.Show("Crie um cliente primeiro."); };
        delete.Click += (_, _) => DeleteSelectedClient();
        clientLayout.Controls.Add(add, 2, 0);
        clientLayout.Controls.Add(edit, 3, 0);
        clientLayout.Controls.Add(delete, 4, 0);
        work.Controls.Add(clientLayout, 0, 1);
        work.Controls.Add(_sequence, 0, 2);
        work.Controls.Add(FileRow("Salvar resultado em", _output, "Vídeo MP4|*.mp4", true), 0, 3);
        work.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Border, Margin = new Padding(0, 8, 0, 8) }, 0, 4);

        var progressTop = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Tag = "surface" };
        progressTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        progressTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        progressTop.Controls.Add(new Label { Text = "PROGRESSO DA RENDERIZAÇÃO", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 8.5f) }, 0, 0);
        progressTop.Controls.Add(new Label { Text = "VÍDEO ORIGINAL PRESERVADO", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 7.5f) }, 1, 0);
        work.Controls.Add(progressTop, 0, 5);
        _progress.Dock = DockStyle.Fill; _progress.Margin = new Padding(0, 7, 0, 7);
        work.Controls.Add(_progress, 0, 6);
        var statusRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Tag = "surface" };
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        _status.Dock = DockStyle.Fill; _status.TextAlign = ContentAlignment.MiddleLeft; _status.ForeColor = AppTheme.Muted;
        statusRow.Controls.Add(_status, 0, 0);
        var ffmpeg = MediaRenderer.FindTool("ffmpeg");
        statusRow.Controls.Add(new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 8.5f), ForeColor = ffmpeg is null ? Color.FromArgb(224, 166, 79) : Color.FromArgb(104, 190, 139), Text = ffmpeg is null ? "MECANISMO NÃO ENCONTRADO" : "SISTEMA PRONTO" }, 1, 0);
        work.Controls.Add(statusRow, 0, 7);
        work.Controls.Add(new Label { Text = "Monte a sequência livremente. A abertura e o encerramento do cliente serão adicionados automaticamente.", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.TopLeft, Padding = new Padding(0, 8, 0, 0) }, 0, 8);

        var actionsHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 10, 28, 18), Tag = "background" };
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Tag = "background" };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        StylePrimary(_render);
        _render.Text = "GERAR VÍDEO"; _render.Dock = DockStyle.Fill; _render.Margin = new Padding(0, 4, 12, 4);
        _cancel.Dock = DockStyle.Fill; _cancel.Margin = new Padding(0, 4, 12, 4);
        _render.Click += async (_, _) => await RenderAsync();
        _cancel.Click += (_, _) => _cts?.Cancel();
        actions.Controls.Add(_render, 0, 0);
        actions.Controls.Add(_cancel, 1, 0);
        actions.Controls.Add(new Label { Text = "QUALIDADE ALTA  •  H.264  •  ÁUDIO AAC", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 8.5f) }, 3, 0);
        actionsHost.Controls.Add(actions);
        root.Controls.Add(actionsHost, 0, 2);
    }

    private Control FileRow(string heading, TextBox box, string filter, bool save)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 2, Tag = "surface" };
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); row.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        row.Controls.Add(new Label { Text = heading, Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.BottomLeft }, 0, 0);
        box.Dock = DockStyle.Fill; box.Margin = new Padding(0, 4, 14, 2); row.Controls.Add(box, 0, 1);
        var browse = new Button { Text = "PROCURAR", Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 2) };
        browse.Click += (_, _) => BrowseForFile(box, filter, save);
        row.Controls.Add(browse, 1, 1);
        return row;
    }

    private void BrowseForFile(TextBox box, string filter, bool save)
    {
        if (save)
        {
            using var dialog = new SaveFileDialog { Filter = filter, DefaultExt = "mp4", AddExtension = true, FileName = SuggestedName() };
            if (dialog.ShowDialog(this) == DialogResult.OK) box.Text = dialog.FileName;
            return;
        }
        using var open = new OpenFileDialog { Filter = filter };
        if (open.ShowDialog(this) != DialogResult.OK) return;
        box.Text = open.FileName;
        if (string.IsNullOrWhiteSpace(_output.Text)) _output.Text = Path.Combine(Path.GetDirectoryName(open.FileName)!, SuggestedName());
    }

    private Panel FileCard(string heading, TextBox box, string filter, bool save)
    {
        var card = Card();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 2, Padding = new Padding(14) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        var label = LabelFor(heading); label.Font = new Font(Font, FontStyle.Bold);
        layout.Controls.Add(label, 0, 0); layout.SetColumnSpan(label, 2);
        box.Dock = DockStyle.Fill;
        layout.Controls.Add(box, 0, 1);
        var browse = new Button { Text = "Procurar...", Dock = DockStyle.Fill };
        browse.Click += (_, _) =>
        {
            if (save)
            {
                using var dialog = new SaveFileDialog { Filter = filter, DefaultExt = "mp4", AddExtension = true, FileName = SuggestedName() };
                if (dialog.ShowDialog(this) == DialogResult.OK) box.Text = dialog.FileName;
            }
            else
            {
                using var dialog = new OpenFileDialog { Filter = filter };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    box.Text = dialog.FileName;
                    if (string.IsNullOrWhiteSpace(_output.Text)) _output.Text = Path.Combine(Path.GetDirectoryName(dialog.FileName)!, SuggestedName());
                }
            }
        };
        layout.Controls.Add(browse, 1, 1);
        card.Controls.Add(layout);
        return card;
    }

    private async Task RenderAsync()
    {
        if (SelectedProfile is not { } profile) { MessageBox.Show("Selecione ou crie um cliente.", "Cliente necessário"); return; }
        if (_sequence.Items.Count == 0 || string.IsNullOrWhiteSpace(_output.Text)) { MessageBox.Show("Adicione pelo menos um vídeo ou imagem à sequência e escolha o arquivo de saída."); return; }
        _cts = new CancellationTokenSource();
        SetBusy(true);
        try
        {
            var progress = new Progress<RenderProgress>(p => { _progress.Value = (int)Math.Clamp(p.Percent, 0, 100); _status.Text = p.Message; });
            await _renderer.RenderAsync(profile, _sequence.Items, _output.Text, progress, _cts.Token);
            Activate();
            if (AppDialog.Confirm(this, "Vídeo concluído", "O vídeo foi gerado com sucesso.\n\nDeseja abrir a pasta e selecionar o arquivo?", "Abrir pasta", "Agora não"))
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_output.Text}\"") { UseShellExecute = true });
        }
        catch (OperationCanceledException) { _status.Text = "Renderização cancelada."; }
        catch (Exception ex) { _status.Text = "Não foi possível gerar o vídeo."; MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); _cts.Dispose(); _cts = null; }
    }

    private void EditClient(ClientProfile? existing)
    {
        var working = existing is null ? new ClientProfile() : Clone(existing);
        using var dialog = new ClientEditorForm(working);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (existing is null) _profiles.Add(working);
        else
        {
            var index = _profiles.IndexOf(existing);
            _profiles[index] = working;
        }
        _store.Save(_profiles);
        RefreshClients(working.Id);
    }

    private void DeleteSelectedClient()
    {
        if (SelectedProfile is not { } profile)
        {
            MessageBox.Show(this, "Selecione um cliente para excluir.", "Cliente necessário");
            return;
        }

        var confirmed = AppDialog.Confirm(this, "Excluir cliente",
            $"Deseja excluir o perfil “{profile.Name}”?\n\nSomente o perfil será removido. Os vídeos, imagens, logos e músicas permanecerão intactos.",
            "Excluir perfil", "Cancelar");
        if (!confirmed) return;

        _profiles.RemoveAll(item => item.Id == profile.Id);
        _store.Save(_profiles);
        RefreshClients();
        _status.Text = $"Perfil “{profile.Name}” excluído.";
    }

    private void RefreshClients(Guid? select = null)
    {
        var id = select ?? SelectedProfile?.Id;
        _clients.DataSource = null;
        _clients.DataSource = _profiles.OrderBy(p => p.Name).ToList();
        if (id.HasValue) _clients.SelectedItem = _clients.Items.Cast<ClientProfile>().FirstOrDefault(p => p.Id == id);
    }

    private string SuggestedName()
    {
        var client = SelectedProfile?.Name ?? "Apresentacao";
        var safe = string.Concat(client.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return $"{safe}_{DateTime.Now:yyyy-MM-dd}.mp4";
    }

    private void SuggestOutputFromSequence()
    {
        if (_sequence.Items.Count == 0 || !string.IsNullOrWhiteSpace(_output.Text)) return;
        var first = _sequence.Items[0].Path;
        var directory = Path.GetDirectoryName(first);
        if (!string.IsNullOrWhiteSpace(directory)) _output.Text = Path.Combine(directory, SuggestedName());
    }

    private ClientProfile? SelectedProfile => _clients.SelectedItem as ClientProfile;
    private void FitToWorkingArea()
    {
        var area = Screen.FromControl(this).WorkingArea;
        Size = new Size(Math.Min(Width, area.Width - 24), Math.Min(Height, area.Height - 24));
        Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
    }
    private void SetBusy(bool busy) { _render.Enabled = !busy; _cancel.Enabled = busy; _clients.Enabled = !busy; _sequence.Enabled = !busy; }
    private static Panel Card() => new() { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Margin = new Padding(0, 5, 0, 5), BorderStyle = BorderStyle.FixedSingle };
    private static Label LabelFor(string text) => new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left };
    private static void StylePrimary(Button b) { b.Tag = "primary"; b.BackColor = AppTheme.Primary; b.ForeColor = AppTheme.Text; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.Font = new Font("Segoe UI Semibold", 11); }

    private static ClientProfile Clone(ClientProfile p) => new()
    {
        Id = p.Id, Name = p.Name, IntroPath = p.IntroPath, OutroPath = p.OutroPath,
        LogoPath = p.LogoPath, MusicPath = p.MusicPath, IntroImageSeconds = p.IntroImageSeconds,
        OutroImageSeconds = p.OutroImageSeconds, FadeSeconds = p.FadeSeconds,
        MusicVolumePercent = p.MusicVolumePercent, OriginalVolumePercent = p.OriginalVolumePercent,
        LogoWidth = p.LogoWidth, LogoMarginLeft = p.LogoMarginLeft, LogoMarginBottom = p.LogoMarginBottom,
        Width = p.Width, Height = p.Height, Crf = p.Crf
    };
}
