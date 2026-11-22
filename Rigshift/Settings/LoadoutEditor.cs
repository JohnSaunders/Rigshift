using System.ComponentModel;

namespace Rigshift.Settings;

/// <summary>
/// Loadouts tab body: the loadout names in a themed list on the left with Add /
/// Duplicate / Delete under it, the selected row's fields on the right (a fixed Name /
/// Mode / Color / Options label column), and the three reference cards side by side,
/// sized to share the field panel's width.
/// Name, color, and checkbox edits write through to the selected draft row as they
/// happen; row structure changes delegate to <see cref="DraftListEditor"/>.
/// </summary>
public class LoadoutEditor : UserControl
{
    private const int ReferenceEditorHeight = 212;
    private const int ReferenceGap = 10;

    private const string StartDescription = "Started when it turns on";
    private const string KillDescription = "Closed when it runs";
    private const string KillOnDisableDescription = "Stopped when it turns off";

    private readonly SettingsDraft _draft;
    private readonly ThemedListBox _listBox = new();
    private readonly FlowLayoutPanel _fieldPanel;
    private readonly FlowLayoutPanel _referencesRow = new()
    {
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = Theme.Surface,
        Margin = new Padding(0, 10, 0, 12)
    };
    private readonly ThemedTextBox _nameBox = new();
    private readonly ThemedCheckBox _toggleBox = new();
    private readonly FlowLayoutPanel _toggleRow;
    private readonly Label _toggleHint = new()
    {
        Text = "Off: a button that runs once.",
        AutoSize = true,
        ForeColor = Theme.Muted,
        BackColor = Theme.Surface,
        Font = Theme.SmallFont,
        Margin = new Padding(12, 0, 2, 0)
    };
    private readonly ColorField _colorField = new();
    private readonly ThemedCheckBox _hardShutdownBox = new();
    private readonly ThemedCheckBox _flushDnsBox = new();
    private readonly ThemedButton _addButton = new() { Text = "Add", AutoSize = true };
    private readonly ThemedButton _duplicateButton = new() { Text = "Duplicate", AutoSize = true };
    private readonly ThemedButton _deleteButton = new() { Text = "Delete", AutoSize = true };
    private readonly Label _hintLabel = new()
    {
        Text = "Select an item from the list or click Add to begin.",
        AutoSize = true,
        ForeColor = Theme.Muted,
        BackColor = Theme.Surface,
        Margin = new Padding(0, 0, 0, 8)
    };

    private ReferenceListEditor _startEditor = NewStartEditor();
    private ReferenceListEditor _killEditor = NewKillEditor();
    private ReferenceListEditor _killOnDisableEditor = NewKillOnDisableEditor();

    // True while a row is being shown or cleared; field handlers skip their
    // draft writes for that duration.
    private bool _loading;

    // True when no row is selected and the empty-state hint is shown. Reported from a
    // flag, not the label's live Visible property, which reports parent-relative state
    // and reads false while the window is not shown.
    private bool _hintVisible;

    /// <summary>When true (default), <see cref="Delete"/> asks before deleting; when false it deletes directly.</summary>
    [DefaultValue(true)]
    public bool ConfirmDelete { get; set; } = true;

    public LoadoutEditor(SettingsDraft draft)
    {
        if (draft is null)
            throw new ArgumentNullException(nameof(draft));
        _draft = draft;

        SuspendLayout();

        var scale = DeviceDpi / 96f;
        _listBox.SelectionMode = SelectionMode.One;
        _listBox.PipColor = PipForIndex;

        // Add / Duplicate / Delete sit under the list as outlined buttons (design §5),
        // in equal columns so all three always fit the list's width.
        _duplicateButton.Enabled = false;
        _deleteButton.Enabled = false;
        var listPanel = EditorLayout.ListPanel(_listBox, scale, _addButton, _duplicateButton, _deleteButton);

        _fieldPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.Surface,
            Padding = new Padding(ThemePainter.Px(16, scale), ThemePainter.Px(14, scale), ThemePainter.Px(16, scale), ThemePainter.Px(8, scale))
        };
        // The three cards share whatever width the field panel has, so they always fit
        // side by side, also when a vertical scroll bar takes some of it.
        _fieldPanel.Layout += (_, _) => SizeReferenceEditors();

        // The fixed label column keeps Name, Mode, Color and Options left edges aligned,
        // and each label is centred on its input.
        _nameBox.Enabled = false;
        _nameBox.Width = 240;
        _nameBox.Margin = new Padding(0, 0, 0, 0);
        var nameRow = EditorLayout.FieldRow("Name", _nameBox);

        _toggleBox.Text = "Show as a switch in the tray";
        _toggleBox.Enabled = false;
        _toggleBox.BackColor = Theme.Surface;
        _toggleBox.Margin = new Padding(0, 0, 0, 0);
        _toggleBox.Height = _nameBox.Height;
        _toggleBox.AutoSize = false;
        _toggleBox.Width = _toggleBox.GetPreferredSize(Size.Empty).Width;
        _toggleRow = EditorLayout.FieldRow("Mode", _toggleBox, _toggleHint);

        _colorField.Enabled = false;
        _colorField.Margin = new Padding(0, 0, 0, 0);
        var colorRow = EditorLayout.FieldRow("Color", _colorField);

        _startEditor.Enabled = false;
        _killEditor.Enabled = false;
        _killOnDisableEditor.Enabled = false;
        _referencesRow.Controls.Add(_startEditor);
        _referencesRow.Controls.Add(_killEditor);
        _referencesRow.Controls.Add(_killOnDisableEditor);

        _hardShutdownBox.Text = "Hard shutdown";
        _hardShutdownBox.Enabled = false;
        _hardShutdownBox.BackColor = Theme.Surface;
        _hardShutdownBox.Margin = new Padding(0, 0, ThemePainter.Px(20, scale), 0);

        _flushDnsBox.Text = "Flush DNS";
        _flushDnsBox.Enabled = false;
        _flushDnsBox.BackColor = Theme.Surface;
        _flushDnsBox.Margin = new Padding(0);

        var checkRow = EditorLayout.FieldRow("Options", _hardShutdownBox, _flushDnsBox);

        _fieldPanel.Controls.Add(_hintLabel);
        _fieldPanel.Controls.Add(nameRow);
        _fieldPanel.Controls.Add(_toggleRow);
        _fieldPanel.Controls.Add(colorRow);
        _fieldPanel.Controls.Add(_referencesRow);
        _fieldPanel.Controls.Add(checkRow);

        _listBox.Items.Clear();
        foreach (var row in draft.Loadouts)
            _listBox.Items.Add(RowText(row.Name));

        _listBox.SelectedIndexChanged += (_, _) =>
        {
            if (_loading)
                return;
            var index = _listBox.SelectedIndex;
            if (index < 0)
            {
                ShowNoSelection();
                return;
            }
            ShowRow(index);
        };

        _nameBox.TextChanged += (_, _) =>
        {
            if (_loading)
                return;
            var index = _listBox.SelectedIndex;
            if (index < 0)
                return;
            _draft.Loadouts[index].Name = _nameBox.Text;
            _loading = true;
            try
            {
                _listBox.Items[index] = RowText(_draft.Loadouts[index].Name);
                _listBox.SelectedIndex = index;
            }
            finally
            {
                _loading = false;
            }
            _listBox.Invalidate();
        };

        _colorField.HexChanged += (_, _) =>
        {
            if (_loading)
                return;
            var index = _listBox.SelectedIndex;
            if (index < 0)
                return;
            _draft.Loadouts[index].Color = _colorField.HexText;
            _listBox.Invalidate();
        };

        _toggleBox.CheckedChanged += (_, _) =>
        {
            if (_loading)
                return;
            var index = _listBox.SelectedIndex;
            if (index < 0)
                return;
            _draft.Loadouts[index].Toggle = _toggleBox.Checked;
        };

        _hardShutdownBox.CheckedChanged += (_, _) =>
        {
            if (_loading)
                return;
            var index = _listBox.SelectedIndex;
            if (index < 0)
                return;
            _draft.Loadouts[index].HardShutdown = _hardShutdownBox.Checked;
        };

        _flushDnsBox.CheckedChanged += (_, _) =>
        {
            if (_loading)
                return;
            var index = _listBox.SelectedIndex;
            if (index < 0)
                return;
            _draft.Loadouts[index].FlushDns = _flushDnsBox.Checked;
        };

        _addButton.Click += (_, _) => Add();
        _duplicateButton.Click += (_, _) => Duplicate();
        _deleteButton.Click += (_, _) => Delete();

        // Docking resolves from the end of the collection: fill first, then the left list.
        Controls.Add(_fieldPanel);
        Controls.Add(listPanel);

        ResumeLayout(false);
        ShowNoSelection();
    }

    /// <summary>The current list-box texts in order; a blank name shows as <c>(unnamed)</c>.</summary>
    public IReadOnlyList<string> LoadoutNames => _listBox.Items.Cast<string>().ToList();

    public int SelectedIndex => _listBox.SelectedIndex;

    /// <summary>
    /// True when nothing is selected and the empty-state hint is shown. Reported from
    /// the <c>SetRowControlsEnabled</c> decision, not the label's live <c>Visible</c>
    /// property, which reports parent-relative state and reads false while the window
    /// is not shown.
    /// </summary>
    public bool HintVisible => _hintVisible;

    /// <summary>The reference editor for the selected row's start list. Empty and disabled before a selection.</summary>
    public ReferenceListEditor StartList => _startEditor;

    /// <summary>The reference editor for the selected row's kill list. Empty and disabled before a selection.</summary>
    public ReferenceListEditor KillList => _killEditor;

    /// <summary>The reference editor for the selected row's kill-on-disable list. Empty and disabled before a selection.</summary>
    public ReferenceListEditor KillOnDisableList => _killOnDisableEditor;

    [DefaultValue(false)]
    public bool ToggleChecked
    {
        get => _toggleBox.Checked;
        set => _toggleBox.Checked = value;
    }

    public string ToggleText => _toggleBox.Text;

    public string ToggleHintText => _toggleHint.Text;

    [DefaultValue(false)]
    public bool HardShutdownChecked
    {
        get => _hardShutdownBox.Checked;
        set => _hardShutdownBox.Checked = value;
    }

    [DefaultValue(false)]
    public bool FlushDnsChecked
    {
        get => _flushDnsBox.Checked;
        set => _flushDnsBox.Checked = value;
    }

    [DefaultValue("")]
    public string NameText
    {
        get => _nameBox.Text;
        set => _nameBox.Text = value ?? "";
    }

    [DefaultValue("")]
    public string ColorText
    {
        get => _colorField.HexText;
        set => _colorField.HexText = value ?? "";
    }

    /// <summary>The color field holding the selected row's hex color.</summary>
    public ColorField ColorField => _colorField;

    /// <summary>
    /// Shows the loadout at <paramref name="index"/>. Fills the field controls from
    /// the row (without writing back) and binds the three reference editors to the
    /// row's lists. An out-of-range index clears the selection instead.
    /// </summary>
    public void Select(int index)
    {
        if (index < 0 || index >= _draft.Loadouts.Count)
        {
            ShowNoSelection();
            return;
        }
        ShowRow(index);
    }

    /// <summary>Appends a new loadout via <see cref="DraftListEditor.AddLoadout"/> and selects it.</summary>
    public void Add()
    {
        DraftListEditor.AddLoadout(_draft);
        _listBox.Items.Add(RowText(_draft.Loadouts[_draft.Loadouts.Count - 1].Name));
        Select(_draft.Loadouts.Count - 1);
        ActiveControl = _nameBox.Inner;
    }

    /// <summary>
    /// Duplicates the selected loadout via <see cref="DraftListEditor.DuplicateLoadout"/>
    /// and selects the copy. Does nothing when nothing is selected.
    /// </summary>
    public void Duplicate()
    {
        var index = _listBox.SelectedIndex;
        if (index < 0)
            return;
        DraftListEditor.DuplicateLoadout(_draft, index);
        _listBox.Items.Insert(index + 1, RowText(_draft.Loadouts[index + 1].Name));
        Select(index + 1);
    }

    /// <summary>
    /// Deletes the selected loadout via <see cref="DraftListEditor.DeleteLoadout"/> after
    /// confirming when <see cref="ConfirmDelete"/> is true. Selects the nearest remaining
    /// row, or clears the fields when the list is empty. Does nothing when nothing is
    /// selected.
    /// </summary>
    public void Delete()
    {
        var index = _listBox.SelectedIndex;
        if (index < 0)
            return;

        if (ConfirmDelete)
        {
            var name = _draft.Loadouts[index].Name?.Trim() ?? "";
            var message = name.Length == 0
                ? "Delete this loadout?"
                : $"Delete loadout '{name}'?";
            if (MessageBox.Show(message, "Rigshift", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
        }

        DraftListEditor.DeleteLoadout(_draft, index);
        _listBox.Items.RemoveAt(index);
        if (_draft.Loadouts.Count == 0)
        {
            ShowNoSelection();
            return;
        }
        Select(Math.Min(index, _draft.Loadouts.Count - 1));
    }

    /// <summary>Calls <see cref="ReferenceListEditor.RefreshKeys"/> on each bound editor.</summary>
    public void RefreshReferenceKeys()
    {
        _startEditor.RefreshKeys();
        _killEditor.RefreshKeys();
        _killOnDisableEditor.RefreshKeys();
    }

    /// <summary>The loadout colour pip for the list row at <paramref name="index"/>; a null colour draws an empty ring.</summary>
    private Color? PipForIndex(int index)
    {
        if (index < 0 || index >= _draft.Loadouts.Count)
            return null;
        return ColorField.TryParseHex(_draft.Loadouts[index].Color, out var color) ? color : null;
    }

    private void ShowRow(int index)
    {
        _loading = true;
        try
        {
            _listBox.ClearSelected();
            _listBox.SelectedIndex = index;
            var row = _draft.Loadouts[index];
            _nameBox.Text = row.Name ?? "";
            _toggleBox.Checked = row.Toggle;
            _colorField.HexText = row.Color ?? "";
            _hardShutdownBox.Checked = row.HardShutdown;
            _flushDnsBox.Checked = row.FlushDns;
            _startEditor.Bind(row.Start, () => GetAvailableKeys());
            _killEditor.Bind(row.Kill, () => GetAvailableKeys());
            _killOnDisableEditor.Bind(row.KillOnDisable, () => GetAvailableKeys());
        }
        finally
        {
            _loading = false;
        }
        SetRowControlsEnabled(true);
    }

    private void ShowNoSelection()
    {
        _loading = true;
        try
        {
            if (_listBox.SelectedIndex >= 0)
                _listBox.ClearSelected();
            _nameBox.Text = "";
            _colorField.HexText = "";
            _toggleBox.Checked = false;
            _hardShutdownBox.Checked = false;
            _flushDnsBox.Checked = false;
        }
        finally
        {
            _loading = false;
        }
        ResetReferenceEditors();
        SetRowControlsEnabled(false);
    }

    // Replaces the editors with fresh empty ones so nothing still shows a previous
    // row's lists. The removed instances are disposed and the row is rebuilt in the
    // fixed horizontal order.
    private void ResetReferenceEditors()
    {
        _referencesRow.SuspendLayout();
        _referencesRow.Controls.Clear();

        _startEditor.Dispose();
        _killEditor.Dispose();
        _killOnDisableEditor.Dispose();
        _startEditor = NewStartEditor();
        _killEditor = NewKillEditor();
        _killOnDisableEditor = NewKillOnDisableEditor();
        _startEditor.Enabled = _killEditor.Enabled = _killOnDisableEditor.Enabled = false;

        _referencesRow.Controls.Add(_startEditor);
        _referencesRow.Controls.Add(_killEditor);
        _referencesRow.Controls.Add(_killOnDisableEditor);
        SizeReferenceEditors();
        _referencesRow.ResumeLayout();
    }

    private static ReferenceListEditor NewStartEditor() =>
        new("Start", StartDescription) { Height = ReferenceEditorHeight, TopicColor = Theme.Teal };

    private static ReferenceListEditor NewKillEditor() =>
        new("Kill", KillDescription) { Height = ReferenceEditorHeight, TopicColor = Theme.Pink };

    private static ReferenceListEditor NewKillOnDisableEditor() =>
        new("Kill on disable", KillOnDisableDescription) { Height = ReferenceEditorHeight, TopicColor = Theme.Amber };

    // Splits the field panel's usable width between the three cards, with a fixed gap
    // between them. Runs before the field panel lays its rows out, so the row it then
    // measures already has the new widths.
    private void SizeReferenceEditors()
    {
        var scale = DeviceDpi / 96f;
        var gap = ThemePainter.Px(ReferenceGap, scale);
        var usable = _fieldPanel.ClientSize.Width - _fieldPanel.Padding.Horizontal - _referencesRow.Margin.Horizontal;
        var width = Math.Max(_startEditor.MinimumSize.Width, (usable - 2 * gap) / 3);

        var editors = new[] { _startEditor, _killEditor, _killOnDisableEditor };
        for (var i = 0; i < editors.Length; i++)
        {
            var editor = editors[i];
            var margin = new Padding(0, 0, i == editors.Length - 1 ? 0 : gap, 0);
            if (editor.Margin != margin)
                editor.Margin = margin;
            if (editor.Width != width)
                editor.Width = width;
        }
    }

    private void SetRowControlsEnabled(bool enabled)
    {
        _hintVisible = !enabled;
        _hintLabel.Visible = !enabled;
        _nameBox.Enabled = enabled;
        _colorField.Enabled = enabled;
        _toggleBox.Enabled = enabled;
        _hardShutdownBox.Enabled = enabled;
        _flushDnsBox.Enabled = enabled;
        _startEditor.Enabled = enabled;
        _killEditor.Enabled = enabled;
        _killOnDisableEditor.Enabled = enabled;
        _duplicateButton.Enabled = enabled;
        _deleteButton.Enabled = enabled;
    }

    private List<string> GetAvailableKeys()
    {
        var keys = new List<string>();
        foreach (var definition in _draft.Definitions)
        {
            var key = definition.Key?.Trim();
            if (string.IsNullOrEmpty(key))
                continue;
            if (keys.IndexOf(key) < 0)
                keys.Add(key);
        }
        return keys;
    }

    private static string RowText(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        return trimmed.Length == 0 ? "(unnamed)" : trimmed;
    }
}

