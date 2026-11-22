using System.ComponentModel;

namespace Rigshift.Settings;

/// <summary>
/// Key list card for the settings form. A bordered card holds a header (the caption and
/// a count badge), an optional one-line description, the bound keys as rows with a remove
/// cross, and an outlined <c>Add</c> button that opens a menu of the keys not yet bound.
/// The Delete key also removes the selected row. Mutates the bound <see cref="List{T}"/>
/// in place; never replaces it.
/// </summary>
public class ReferenceListEditor : UserControl
{
    /// <summary>The empty-state text shown when no key is bound.</summary>
    public const string EmptyListText = "Nothing added";

    private const int MinimumCardWidth = 120;

    private readonly ThemedListBox _list = new();
    private readonly ThemedButton _addButton = new() { Text = "Add", ShowPlus = true, AutoSize = false };
    private readonly CountBadge _countBadge = new();
    private readonly TopicDot _dot = new();
    private Color _topicColor = Theme.Accent;
    private readonly Label _captionLabel = new();
    private readonly Label _descriptionLabel = new();
    private readonly ThemedMenu _menu = new();
    private readonly List<string> _choices = new();
    private List<string>? _items;
    private Func<IReadOnlyList<string>>? _availableKeys;

    public ReferenceListEditor(string caption) : this(caption, null)
    {
    }

    public ReferenceListEditor(string caption, string? description)
    {
        SuspendLayout();

        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        BackColor = Theme.Card;
        var scale = DeviceDpi / 96f;
        var frame = ThemePainter.Px(1, scale);
        Padding = new Padding(frame);

        _captionLabel.Text = caption;
        _captionLabel.Font = Theme.UiSemibold;
        _captionLabel.ForeColor = Theme.Text;
        _captionLabel.BackColor = Theme.Card;
        _captionLabel.AutoSize = true;
        _captionLabel.Anchor = AnchorStyles.Left;
        _captionLabel.Margin = Padding.Empty;

        _countBadge.Anchor = AnchorStyles.Right;
        _countBadge.Margin = Padding.Empty;

        _dot.Anchor = AnchorStyles.Left;
        _dot.Margin = new Padding(0, 0, ThemePainter.Px(7, scale), 0);
        _dot.Size = new Size(ThemePainter.Px(7, scale), ThemePainter.Px(7, scale));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Card,
            Padding = new Padding(ThemePainter.Px(10, scale), ThemePainter.Px(8, scale), ThemePainter.Px(8, scale), ThemePainter.Px(2, scale))
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(_dot, 0, 0);
        header.Controls.Add(_captionLabel, 1, 0);
        header.Controls.Add(_countBadge, 2, 0);

        _descriptionLabel.Text = description ?? "";
        _descriptionLabel.Visible = !string.IsNullOrEmpty(description);
        _descriptionLabel.Dock = DockStyle.Top;
        _descriptionLabel.AutoSize = false;
        _descriptionLabel.AutoEllipsis = true;
        _descriptionLabel.Font = Theme.SmallFont;
        _descriptionLabel.ForeColor = Theme.Muted;
        _descriptionLabel.BackColor = Theme.Card;
        // A Label draws its text a few pixels in from its padding; the left pad lines the
        // description up under the caption.
        _descriptionLabel.Padding = new Padding(ThemePainter.Px(7, scale), 0, ThemePainter.Px(8, scale), 0);
        _descriptionLabel.Height = ThemePainter.Px(20, scale);
        _descriptionLabel.TextAlign = ContentAlignment.TopLeft;

        var rule = new Panel { Dock = DockStyle.Top, Height = frame, BackColor = Theme.Border };

        _list.Dock = DockStyle.Fill;
        _list.BackColor = Theme.Card;
        _list.EmptyText = EmptyListText;
        _list.ShowRemoveGlyph = true;
        _list.AccessibleName = caption;
        _list.AccessibleDescription = "Press Delete to remove the selected item.";
        _list.RemoveClicked += (_, index) => RemoveAt(index);
        // Delete removes the selected entry, alongside the row's remove cross.
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                RemoveAt(_list.SelectedIndex);
                e.Handled = true;
            }
        };

        var footerPad = ThemePainter.Px(8, scale);
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            BackColor = Theme.Card,
            Padding = new Padding(footerPad, ThemePainter.Px(4, scale), footerPad, footerPad)
        };
        _addButton.Dock = DockStyle.Fill;
        _addButton.Height = ThemePainter.Px(Theme.ButtonHeight, scale);
        footer.Height = _addButton.Height + footer.Padding.Vertical;
        footer.Controls.Add(_addButton);
        _addButton.Click += (_, _) => ShowAddMenu();

        // Docking resolves from the end of the collection: the header claims the top
        // first, then the description and the rule; the footer takes the bottom and the
        // list fills what is left.
        Controls.Add(_list);
        Controls.Add(footer);
        Controls.Add(rule);
        Controls.Add(_descriptionLabel);
        Controls.Add(header);

        MinimumSize = new Size(ThemePainter.Px(MinimumCardWidth, scale), MinimumSize.Height);
        Width = Math.Max(Width, MinimumSize.Width);

        UpdateCount();
        ResumeLayout(false);
    }

    /// <summary>The bound entries in order. Empty before <see cref="Bind"/>.</summary>
    public IReadOnlyList<string> Items => _items ?? (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>The keys the Add menu offers, in order: available keys not already bound.</summary>
    public IReadOnlyList<string> AvailableChoices => _choices.ToList();

    /// <summary>The count shown in the header badge, e.g. <c>2</c>.</summary>
    public string CountText => _countBadge.Text;

    /// <summary>
    /// The card's topic colour, one of the site's: the dot by the title and the count
    /// badge take it. Defaults to the accent.
    /// </summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TopicColor
    {
        get => _topicColor;
        set
        {
            _topicColor = value;
            _dot.Color = value;
            _countBadge.Topic = value;
        }
    }

    /// <summary>The card title.</summary>
    public string Caption => _captionLabel.Text;

    /// <summary>The muted line under the title; empty when there is none.</summary>
    public string Description => _descriptionLabel.Text;

    /// <summary>The menu the Add button opens. Rebuilt each time it is shown.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ContextMenuStrip AddMenu => _menu;

    /// <summary>
    /// Stores the list and the available-keys source, loads the rows from the list, and
    /// refreshes the Add choices. The list is mutated in place afterwards.
    /// </summary>
    public void Bind(List<string> items, Func<IReadOnlyList<string>> availableKeys)
    {
        _items = items;
        _availableKeys = availableKeys;

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var item in items)
            _list.Items.Add(item);
        _list.EndUpdate();

        RefreshKeys();
        UpdateCount();
    }

    /// <summary>
    /// Appends the key to the bound list and the rows when it is non-blank and not
    /// already present (ordinal, untrimmed), then refreshes the Add choices.
    /// Does nothing when the key is null, blank, or already present.
    /// </summary>
    public void AddKey(string? key)
    {
        if (_items is null || string.IsNullOrWhiteSpace(key))
            return;

        if (_items.Any(item => string.Equals(item, key, StringComparison.Ordinal)))
            return;

        _items.Add(key);
        _list.Items.Add(key);
        RefreshKeys();
        UpdateCount();
    }

    /// <summary>
    /// Removes the entry at <paramref name="index"/> from the bound list and the rows,
    /// and offers it in the Add menu again. Does nothing when the index is outside the list.
    /// </summary>
    public void RemoveAt(int index)
    {
        if (_items is null || index < 0 || index >= _items.Count)
            return;

        _items.RemoveAt(index);
        _list.Items.RemoveAt(index);
        RefreshKeys();
        UpdateCount();
    }

    /// <summary>Rebuilds the Add choices from the available keys, excluding the bound keys (ordinal).</summary>
    public void RefreshKeys()
    {
        if (_items is null || _availableKeys is null)
            return;

        _choices.Clear();
        foreach (var key in _availableKeys())
        {
            if (!_items.Any(item => string.Equals(item, key, StringComparison.Ordinal)) && !_choices.Contains(key))
                _choices.Add(key);
        }
    }

    /// <summary>
    /// Rebuilds <see cref="AddMenu"/> from <see cref="AvailableChoices"/> and opens it under
    /// the Add button. With nothing left to add it shows one disabled line saying why.
    /// </summary>
    public void ShowAddMenu()
    {
        BuildMenu();
        if (!IsHandleCreated || !_addButton.IsHandleCreated)
            return;
        _menu.MinimumSize = new Size(_addButton.Width, 0);
        _menu.Show(_addButton, new Point(0, _addButton.Height));
    }

    private void BuildMenu()
    {
        _menu.Items.Clear();
        if (_choices.Count == 0)
        {
            var anyKeys = (_availableKeys?.Invoke().Count ?? 0) > 0;
            _menu.Items.Add(new ToolStripMenuItem(anyKeys ? "Everything is already added" : "No definitions yet") { Enabled = false });
            return;
        }

        foreach (var key in _choices)
        {
            var choice = key;
            var item = new ToolStripMenuItem(choice.Replace("&", "&&"));
            item.Click += (_, _) => AddKey(choice);
            _menu.Items.Add(item);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        ThemePainter.FillFrame(e.Graphics, ClientRectangle, Theme.Card, Theme.Border, DeviceDpi / 96f);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        _captionLabel.ForeColor = Enabled ? Theme.Text : Theme.Muted;
        _countBadge.Invalidate();
        _dot.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _menu.Dispose();
        base.Dispose(disposing);
    }

    private void UpdateCount() => _countBadge.Text = (_items?.Count ?? 0).ToString();

    /// <summary>
    /// The count pill in a card header: accent on an accent tint when the list has
    /// entries, muted on the card when it is empty or disabled.
    /// </summary>
    private sealed class CountBadge : Control
    {
        private Color _topic = Theme.Accent;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Topic
        {
            get => _topic;
            set
            {
                _topic = value;
                Invalidate();
            }
        }

        public CountBadge()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
                true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Theme.Card;
            Font = Theme.MonoFont;
            TabStop = false;
            Text = "0";
        }

        private float DpiScale => DeviceDpi / 96f;

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Size = GetPreferredSize(Size.Empty);
            Invalidate();
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var scale = DpiScale;
            var text = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            var height = ThemePainter.Px(18, scale);
            var width = Math.Max(ThemePainter.Px(26, scale), text.Width + ThemePainter.Px(14, scale));
            return new Size(width, height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var back = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(back, ClientRectangle);

            var active = Enabled && Text != "0";
            var bounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            if (active)
                ThemePainter.DrawPill(e.Graphics, bounds, Text, Font, Theme.Blend(_topic, Theme.Card, 0.14), Theme.Blend(_topic, Theme.Card, 0.45), _topic, DpiScale);
            else
                ThemePainter.DrawPill(e.Graphics, bounds, Text, Font, Theme.Card, Theme.Border, Theme.Muted, DpiScale);
        }
    }

    /// <summary>The round topic dot before a card title, as on the site's topic chips.</summary>
    private sealed class TopicDot : Control
    {
        private Color _color = Theme.Accent;

        public TopicDot()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
                true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Theme.Card;
            TabStop = false;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var back = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(back, ClientRectangle);
            var parentEnabled = Parent?.Enabled ?? true;
            ThemePainter.FillCapsule(e.Graphics, ClientRectangle, Enabled && parentEnabled ? _color : Theme.Border);
        }
    }
}
