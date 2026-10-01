namespace VFL.VideoApresentacao;

internal sealed class MediaSequenceControl : UserControl
{
    private readonly List<MediaSequenceItem> _items = [];
    private readonly ListView _list = new()
    {
        Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
        MultiSelect = true, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable
    };

    public IReadOnlyList<MediaSequenceItem> Items => _items;
    public event EventHandler? SequenceChanged;

    public MediaSequenceControl()
    {
        Dock = DockStyle.Fill;
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Tag = "surface" };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 144));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var title = new Label { Text = "SEQUÊNCIA DA APRESENTAÇÃO", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, Font = new Font("Segoe UI Semibold", 8.5f), TextAlign = ContentAlignment.MiddleLeft };
        var help = new Label { Text = "Duplo clique edita imagens", Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, Font = new Font("Segoe UI", 8), TextAlign = ContentAlignment.MiddleRight };
        root.Controls.Add(title, 0, 0); root.Controls.Add(help, 1, 0);

        _list.Columns.Add("#", 38, HorizontalAlignment.Center);
        _list.Columns.Add("TIPO", 78);
        _list.Columns.Add("ARQUIVO", 410);
        _list.Columns.Add("DURAÇÃO", 120);
        _list.DoubleClick += (_, _) => EditSelectedImageDuration();
        _list.Resize += (_, _) => ResizeFileColumn();
        root.Controls.Add(_list, 0, 1);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, Tag = "surface", Padding = new Padding(10, 0, 0, 0) };
        for (var i = 0; i < 6; i++) buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666f));
        var addVideos = ActionButton("+ VÍDEOS", (_, _) => AddVideos());
        var addImages = ActionButton("+ IMAGENS", (_, _) => AddImages());
        var up = ActionButton("▲  SUBIR", (_, _) => MoveSelected(-1));
        var down = ActionButton("▼  DESCER", (_, _) => MoveSelected(1));
        var duration = ActionButton("DURAÇÃO", (_, _) => EditSelectedImageDuration());
        var remove = ActionButton("REMOVER", (_, _) => RemoveSelected());
        buttons.Controls.Add(addVideos, 0, 0); buttons.Controls.Add(addImages, 0, 1);
        buttons.Controls.Add(up, 0, 2); buttons.Controls.Add(down, 0, 3);
        buttons.Controls.Add(duration, 0, 4); buttons.Controls.Add(remove, 0, 5);
        root.Controls.Add(buttons, 1, 1);
        Controls.Add(root);
    }

    private static Button ActionButton(string text, EventHandler click)
    {
        var button = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2), Font = new Font("Segoe UI Semibold", 8.5f) };
        button.Click += click;
        return button;
    }

    private void AddVideos()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Vídeos|*.mp4;*.mov;*.mkv;*.avi;*.webm;*.m4v|Todos os arquivos|*.*",
            Multiselect = true, Title = "Adicionar vídeos à sequência"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var path in dialog.FileNames) _items.Add(new MediaSequenceItem { Path = path });
        RefreshList(_items.Count - 1);
    }

    private void AddImages()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Imagens|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Todos os arquivos|*.*",
            Multiselect = true, Title = "Adicionar imagens à sequência"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var path in dialog.FileNames)
        {
            using var duration = new ImageDurationDialog(Path.GetFileName(path), 4);
            if (duration.ShowDialog(this) != DialogResult.OK) continue;
            _items.Add(new MediaSequenceItem { Path = path, ImageSeconds = duration.Seconds });
        }
        RefreshList(_items.Count - 1);
    }

    private void EditSelectedImageDuration()
    {
        if (_list.SelectedIndices.Count != 1) return;
        var index = _list.SelectedIndices[0];
        var item = _items[index];
        if (!item.IsImage) return;
        using var dialog = new ImageDurationDialog(Path.GetFileName(item.Path), item.ImageSeconds);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        item.ImageSeconds = dialog.Seconds;
        RefreshList(index);
    }

    private void RemoveSelected()
    {
        if (_list.SelectedIndices.Count == 0) return;
        var indices = _list.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToArray();
        foreach (var index in indices) _items.RemoveAt(index);
        RefreshList(Math.Min(indices.Min(), _items.Count - 1));
    }

    private void MoveSelected(int direction)
    {
        if (_list.SelectedIndices.Count != 1) return;
        var index = _list.SelectedIndices[0];
        var target = index + direction;
        if (target < 0 || target >= _items.Count) return;
        (_items[index], _items[target]) = (_items[target], _items[index]);
        RefreshList(target);
    }

    private void RefreshList(int selectIndex = -1)
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        for (var i = 0; i < _items.Count; i++)
        {
            var media = _items[i];
            var row = new ListViewItem((i + 1).ToString()) { Tag = media };
            row.SubItems.Add(media.TypeLabel);
            row.SubItems.Add(Path.GetFileName(media.Path));
            row.SubItems.Add(media.DurationLabel);
            _list.Items.Add(row);
        }
        _list.EndUpdate();
        ResizeFileColumn();
        if (selectIndex >= 0 && selectIndex < _list.Items.Count)
        {
            _list.Items[selectIndex].Selected = true;
            _list.Items[selectIndex].EnsureVisible();
        }
        SequenceChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ResizeFileColumn()
    {
        if (_list.Columns.Count < 4) return;
        _list.Columns[2].Width = Math.Max(160, _list.ClientSize.Width - 38 - 78 - 120 - 8);
    }
}
