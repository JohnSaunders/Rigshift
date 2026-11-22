using System.ComponentModel;

namespace Rigshift.Settings;

/// <summary>
/// Definitions tab body: the definition keys in a themed list on the left with Add /
/// Duplicate / Delete under it, and the selected row's fields on the right in a fixed
/// label column, with the Requires card below them. The field
/// edits write through to the selected draft row as they happen; the key commits through
/// <see cref="DraftListEditor.TryRenameDefinitionKey"/> when the box loses focus.
/// </summary>
public class DefinitionEditor : UserControl
{
    private const int ReferenceEditorHeight = 212;
    private const int ReferenceEditorWidth = 280;
    private const string RequiresDescription = "Started before this one";

    private readonly SettingsDraft _draft;
    private readonly ThemedListBox _listBox = new();
    private readonly FlowLayoutPanel _fieldPanel;
    private readonly ThemedTextBox _keyBox = new();
    private readonly ComboBox _kindCombo = new();
    private readonly ThemedTextBox _processBox = new();
    private readonly ThemedTextBox _pathBox = new();
    private readonly ThemedTextBox _windowTitleBox = new();
    private readonly ThemedTextBox _portBox = new();
    private readonly ThemedTextBox _wslCommandBox = new();
    private readonly ThemedCheckBox _portProxyBox = new();
    private readonly Label _keyErrorLabel = new()
    {
        AutoSize = true,
        ForeColor = Theme.Caution,
        Margin = new Padding(0, 4, 0, 4)
    };
    private readonly ThemedButton _addButton = new() { Text = "Add", AutoSize = true };
    private readonly ThemedButton _duplicateButton = new() { Text = "Duplicate", AutoSize = true };
    private readonly ThemedButton _deleteButton = new() { Text = "Delete", AutoSize = true };
    private readonly Label _hintLabel = new()
    {
        Text = "Select an item from the list or click Add to begin.",
        AutoSize = true,
        ForeColor = Theme.Muted,
        Margin = new Padding(0, 0, 0, 8)
    };

    private ReferenceListEditor _requiresEditor = NewRequiresEditor();

    // True while a row is being shown or cleared; field handlers skip their
    // draft writes for that duration.
    private bool _loading;

    private bool _hintVisible;

    /// <summary>When true (default), <see cref="Delete"/> asks before deleting; when false it deletes directly.</summary>
    [DefaultValue(true)]
    public bool ConfirmDelete { get; set; } = true;

    /// <summary>Raised after a successful add, duplicate, delete, or committed rename.</summary>
    public event EventHandler? DefinitionsChanged
    {
        add { _definitionsChanged += value; }
        remove { _definitionsChanged -= value; }
    }

    private EventHandler? _definitionsChanged;

    public DefinitionEditor(SettingsDraft draft)
    {
        if (draft is null)
            throw new ArgumentNullException(nameof(draft));
        _draft = draft;

        SuspendLayout();

        var scale = DeviceDpi / 96f;
        _listBox.SelectionMode = SelectionMode.One;
        _listBox.EmptyText = "None";

        // Add / Duplicate / Delete sit under the list, matching the Loadouts tab.
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

        _keyBox.Enabled = false;
        _keyBox.Width = 240;
        _keyBox.Inner.Leave += (_, _) => CommitPending();

        _kindCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _kindCombo.Width = 110;
        _kindCombo.Enabled = false;
        _kindCombo.FlatStyle = FlatStyle.Flat;
        _kindCombo.BackColor = Theme.Surface;
        _kindCombo.ForeColor = Theme.Text;
        _kindCombo.Items.AddRange("app", "wsl", "wslService", "herdr");

        _processBox.Enabled = false;
        _processBox.Width = 240;
        _pathBox.Enabled = false;
        _pathBox.Width = 240;
        _windowTitleBox.Enabled = false;
        _windowTitleBox.Width = 240;
        _portBox.Enabled = false;
        _portBox.Width = 90;
        _wslCommandBox.Enabled = false;
        _wslCommandBox.Width = 240;

        foreach (var box in new[] { _keyBox, _processBox, _pathBox, _windowTitleBox, _portBox, _wslCommandBox })
            box.Margin = Padding.Empty;
        _kindCombo.Margin = Padding.Empty;

        _portProxyBox.Text = "Port proxy";
        _portProxyBox.Enabled = false;
        _portProxyBox.BackColor = Theme.Surface;
        _portProxyBox.Margin = Padding.Empty;

        _requiresEditor.Enabled = false;

        _fieldPanel.Controls.Add(_hintLabel);
        _fieldPanel.Controls.Add(FieldRow("Key", _keyBox));
        _fieldPanel.Controls.Add(FieldRow("Kind", _kindCombo));
        _fieldPanel.Controls.Add(FieldRow("Process name", _processBox));
        _fieldPanel.Controls.Add(FieldRow("Path", _pathBox));
        _fieldPanel.Controls.Add(FieldRow("Window title", _windowTitleBox));
        _fieldPanel.Controls.Add(FieldRow("Port", _portBox));
        _fieldPanel.Controls.Add(FieldRow("WSL command", _wslCommandBox));
        _fieldPanel.Controls.Add(FieldRow("Options", _portProxyBox));
        _fieldPanel.Controls.Add(_requiresEditor);
        _fieldPanel.Controls.Add(_keyErrorLabel);

        _listBox.Items.Clear();
        foreach (var row in draft.Definitions)
            _listBox.Items.Add(RowText(row.Key));

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

        _kindCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_loading)
                return;
            var index = _listBox.SelectedIndex;
            if (index < 0)
                return;
            _draft.Definitions[index].Kind = _kindCombo.SelectedItem as string ?? "";
        };

        OnOptionalTextChanged(_processBox, (row, text) => row.ProcessName = text);
        OnOptionalTextChanged(_pathBox, (row, text) => row.Path = text);
        OnOptionalTextChanged(_windowTitleBox, (row, text) => row.WindowTitle = text);
        OnOptionalTextChanged(_portBox, (row, text) => row.Port = text);
        OnOptionalTextChanged(_wslCommandBox, (row, text) => row.WslCommand = text);

        _portProxyBox.CheckedChanged += (_, _) =>
        {
            if (_loading)
                return;
            var index = _listBox.SelectedIndex;
            if (index < 0)
                return;
            _draft.Definitions[index].PortProxy = _portProxyBox.Checked;
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

    /// <summary>The current list-box texts in order; a blank key shows as <c>(unnamed)</c>.</summary>
    public IReadOnlyList<string> DefinitionKeys => _listBox.Items.Cast<string>().ToList();

    public int SelectedIndex => _listBox.SelectedIndex;

    /// <summary>
    /// True when nothing is selected and the empty-state hint is shown. Reported from
    /// the <c>SetRowControlsEnabled</c> decision, not the label's live <c>Visible</c>.
    /// </summary>
    public bool HintVisible => _hintVisible;

    /// <summary>The reference editor for the selected row's requires list. Empty and disabled before a selection.</summary>
    public ReferenceListEditor RequiresList => _requiresEditor;

    /// <summary>The colour of the key error line.</summary>
    public Color KeyErrorColor => _keyErrorLabel.ForeColor;

    /// <summary>The latest rename error, or empty when there is none.</summary>
    [DefaultValue("")]
    public string KeyErrorText
    {
        get => _keyErrorLabel.Text;
        set => _keyErrorLabel.Text = value ?? "";
    }

    [DefaultValue("")]
    public string KeyText
    {
        get => _keyBox.Text;
        set => _keyBox.Text = value ?? "";
    }

    [DefaultValue("")]
    public string KindText
    {
        get => _kindCombo.SelectedItem as string ?? "";
        set
        {
            if (value is null)
                return;
            if (_kindCombo.Items.IndexOf(value) < 0)
                _kindCombo.Items.Add(value);
            _kindCombo.SelectedItem = value;
        }
    }

    [DefaultValue("")]
    public string ProcessNameText
    {
        get => _processBox.Text;
        set => _processBox.Text = value ?? "";
    }

    [DefaultValue("")]
    public string PathText
    {
        get => _pathBox.Text;
        set => _pathBox.Text = value ?? "";
    }

    [DefaultValue("")]
    public string WindowTitleText
    {
        get => _windowTitleBox.Text;
        set => _windowTitleBox.Text = value ?? "";
    }

    [DefaultValue("")]
    public string PortText
    {
        get => _portBox.Text;
        set => _portBox.Text = value ?? "";
    }

    [DefaultValue("")]
    public string WslCommandText
    {
        get => _wslCommandBox.Text;
        set => _wslCommandBox.Text = value ?? "";
    }

    [DefaultValue(false)]
    public bool PortProxyChecked
    {
        get => _portProxyBox.Checked;
        set => _portProxyBox.Checked = value;
    }

    /// <summary>
    /// Shows the definition at <paramref name="index"/>, filling the field controls
    /// from the row and binding the requires editor. An out-of-range index clears.
    /// </summary>
    public void Select(int index)
    {
        if (index < 0 || index >= _draft.Definitions.Count)
        {
            ShowNoSelection();
            return;
        }
        ShowRow(index);
    }

    /// <summary>Appends a new definition via <see cref="DraftListEditor.AddDefinition"/> and selects it.</summary>
    public void Add()
    {
        DraftListEditor.AddDefinition(_draft);
        _listBox.Items.Add(RowText(_draft.Definitions[_draft.Definitions.Count - 1].Key));
        Select(_draft.Definitions.Count - 1);
        RaiseDefinitionsChanged();
        ActiveControl = _keyBox.Inner;
    }

    /// <summary>
    /// Duplicates the selected definition via <see cref="DraftListEditor.DuplicateDefinition"/>
    /// and selects the copy. Does nothing when nothing is selected.
    /// </summary>
    public void Duplicate()
    {
        var index = _listBox.SelectedIndex;
        if (index < 0)
            return;
        DraftListEditor.DuplicateDefinition(_draft, index);
        _listBox.Items.Insert(index + 1, RowText(_draft.Definitions[index + 1].Key));
        Select(index + 1);
        RaiseDefinitionsChanged();
    }

    /// <summary>
    /// Deletes the selected definition after a confirmation when <see cref="ConfirmDelete"/>
    /// is true. <see cref="DraftListEditor.TryDeleteDefinition"/> refuses the delete and shows
    /// its message when another loadout or definition still uses the key. Selects the nearest
    /// remaining row, or clears the fields when the list is empty. Raises <see cref="DefinitionsChanged"/>.
    /// </summary>
    public void Delete()
    {
        var index = _listBox.SelectedIndex;
        if (index < 0)
            return;

        if (ConfirmDelete)
        {
            var key = _draft.Definitions[index].Key?.Trim() ?? "";
            var message = key.Length == 0
                ? "Delete this definition?"
                : $"Delete definition '{key}'?";
            if (MessageBox.Show(message, "Rigshift", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
        }

        var error = DraftListEditor.TryDeleteDefinition(_draft, index);
        if (error is not null)
        {
            MessageBox.Show(error, "Rigshift");
            return;
        }

        _listBox.Items.RemoveAt(index);
        if (_draft.Definitions.Count == 0)
            ShowNoSelection();
        else
            Select(Math.Min(index, _draft.Definitions.Count - 1));
        RaiseDefinitionsChanged();
    }

    /// <summary>
    /// Commits the key through <see cref="DraftListEditor.TryRenameDefinitionKey"/>.
    /// Returns null on success (or unchanged), raising <see cref="DefinitionsChanged"/> when
    /// the key changed; returns the error, restores the stored key, and shows it otherwise.
    /// </summary>
    public string? CommitPending()
    {
        var index = _listBox.SelectedIndex;
        if (index < 0)
            return null;

        var row = _draft.Definitions[index];
        var previousKey = row.Key;
        var error = DraftListEditor.TryRenameDefinitionKey(_draft, index, _keyBox.Text);
        if (error is not null)
        {
            _loading = true;
            try
            {
                _keyBox.Text = previousKey;
            }
            finally
            {
                _loading = false;
            }
            _keyErrorLabel.Text = error;
            return error;
        }

        _keyErrorLabel.Text = "";
        if (row.Key != previousKey)
        {
            _listBox.Items[index] = RowText(row.Key);
            RaiseDefinitionsChanged();
        }
        return null;
    }

    private void ShowRow(int index)
    {
        _loading = true;
        try
        {
            _listBox.ClearSelected();
            _listBox.SelectedIndex = index;
            var row = _draft.Definitions[index];
            _keyBox.Text = row.Key ?? "";
            var kind = row.Kind ?? "";
            if (_kindCombo.Items.IndexOf(kind) < 0)
                _kindCombo.Items.Add(kind);
            _kindCombo.SelectedItem = kind;
            _processBox.Text = row.ProcessName ?? "";
            _pathBox.Text = row.Path ?? "";
            _windowTitleBox.Text = row.WindowTitle ?? "";
            _portBox.Text = row.Port ?? "";
            _wslCommandBox.Text = row.WslCommand ?? "";
            _portProxyBox.Checked = row.PortProxy;
            _requiresEditor.Bind(row.Requires, GetAvailableKeys);
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
            _keyBox.Text = "";
            _processBox.Text = "";
            _pathBox.Text = "";
            _windowTitleBox.Text = "";
            _portBox.Text = "";
            _wslCommandBox.Text = "";
            _portProxyBox.Checked = false;
        }
        finally
        {
            _loading = false;
        }
        ResetRequiresEditor();
        SetRowControlsEnabled(false);
    }

    private void ResetRequiresEditor()
    {
        _fieldPanel.SuspendLayout();
        _fieldPanel.Controls.Remove(_requiresEditor);
        _requiresEditor.Dispose();
        _requiresEditor = NewRequiresEditor();
        _requiresEditor.Enabled = false;
        // Back in its place above the key error line, so no blank line opens above the card.
        _fieldPanel.Controls.Add(_requiresEditor);
        _fieldPanel.Controls.SetChildIndex(_requiresEditor, _fieldPanel.Controls.GetChildIndex(_keyErrorLabel));
        _fieldPanel.ResumeLayout();
    }

    private void SetRowControlsEnabled(bool enabled)
    {
        _hintVisible = !enabled;
        _hintLabel.Visible = !enabled;
        _keyBox.Enabled = enabled;
        _kindCombo.Enabled = enabled;
        _processBox.Enabled = enabled;
        _pathBox.Enabled = enabled;
        _windowTitleBox.Enabled = enabled;
        _portBox.Enabled = enabled;
        _wslCommandBox.Enabled = enabled;
        _portProxyBox.Enabled = enabled;
        _requiresEditor.Enabled = enabled;
        _duplicateButton.Enabled = enabled;
        _deleteButton.Enabled = enabled;
    }

    private void OnOptionalTextChanged(ThemedTextBox box, Action<DefinitionDraft, string?> write)
    {
        box.TextChanged += (_, _) =>
        {
            if (_loading)
                return;
            var index = _listBox.SelectedIndex;
            if (index < 0)
                return;
            write(_draft.Definitions[index], Store(box.Text));
        };
    }

    private static string? Store(string text)
        => string.IsNullOrWhiteSpace(text) ? null : text;

    private void RaiseDefinitionsChanged()
        => _definitionsChanged?.Invoke(this, EventArgs.Empty);

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

    private static FlowLayoutPanel FieldRow(string caption, Control input) => EditorLayout.FieldRow(caption, input);

    private static ReferenceListEditor NewRequiresEditor() =>
        new("Requires", RequiresDescription)
        {
            TopicColor = Theme.Blue,
            Height = ReferenceEditorHeight,
            Width = ReferenceEditorWidth,
            Margin = new Padding(0, 10, 0, 4)
        };

    private static string RowText(string? key)
    {
        var trimmed = key?.Trim() ?? "";
        return trimmed.Length == 0 ? "(unnamed)" : trimmed;
    }
}

