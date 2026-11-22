namespace Rigshift.Settings;

/// <summary>
/// Layout pieces the Loadouts and Definitions tabs share, so both tabs line up the same
/// way: the list column on the left with its action buttons, and label + input rows whose
/// labels sit in one fixed-width column, vertically centred on their input.
/// </summary>
public static class EditorLayout
{
    private const int FieldLabelGap = 8;

    /// <summary>
    /// The left column of a tab: <paramref name="list"/> filling it, the buttons in equal
    /// columns along the bottom, and a one-pixel border rule down the right edge.
    /// </summary>
    public static Panel ListPanel(ThemedListBox list, float scale, params ThemedButton[] buttons)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Left,
            Width = ThemePainter.Px(Theme.ListPanelWidth, scale),
            BackColor = Theme.Surface
        };

        var rule = new Panel { Dock = DockStyle.Right, Width = ThemePainter.Px(1, scale), BackColor = Theme.Border };

        var pad = ThemePainter.Px(6, scale);
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = buttons.Length,
            RowCount = 1,
            BackColor = Theme.Surface,
            Padding = new Padding(pad, pad, pad, pad)
        };
        var gap = ThemePainter.Px(2, scale);
        for (var i = 0; i < buttons.Length; i++)
        {
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / buttons.Length));
            var button = buttons[i];
            button.AutoSize = false;
            button.Dock = DockStyle.Fill;
            button.Height = ThemePainter.Px(Theme.ButtonHeight, scale);
            button.Margin = new Padding(i == 0 ? 0 : gap, 0, i == buttons.Length - 1 ? 0 : gap, 0);
            bar.Controls.Add(button, i, 0);
        }

        list.Dock = DockStyle.Fill;

        // Docking resolves from the end of the collection: the rule takes the right edge,
        // the button bar the bottom, and the list fills the rest.
        panel.Controls.Add(list);
        panel.Controls.Add(bar);
        panel.Controls.Add(rule);
        return panel;
    }

    /// <summary>
    /// A field row: <paramref name="caption"/> in the fixed label column, then each input
    /// left to right. Every label and plain text in the row is as tall as the first input
    /// and centred on it, so the text lines up with the input's text.
    /// </summary>
    public static FlowLayoutPanel FieldRow(string caption, params Control[] inputs)
    {
        if (inputs.Length == 0)
            throw new ArgumentException("A field row needs at least one input.", nameof(inputs));

        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Surface,
            Margin = new Padding(0, 0, 0, ThemePainter.Px(6, inputs[0].DeviceDpi / 96f))
        };

        var lead = inputs[0];
        // An auto-sized input has not taken its size yet (a UserControl starts 150px tall),
        // so size it now and centre the label on that.
        if (lead.AutoSize)
            lead.Size = lead.GetPreferredSize(Size.Empty);
        var height = lead.Height;
        row.Controls.Add(FieldLabel(caption, lead.Margin.Top, height));
        foreach (var input in inputs)
        {
            if (input is Label label && !ReferenceEquals(input, lead))
                CentreOn(label, lead.Margin.Top, height);
            row.Controls.Add(input);
        }
        return row;
    }

    /// <summary>An eyebrow label in the fixed label column, <paramref name="height"/> tall, text centred vertically.</summary>
    public static Label FieldLabel(string text, int top, int height) => new EyebrowLabel
    {
        Text = text,
        AutoSize = false,
        Width = ThemePainter.Px(Theme.LabelColumnWidth, 1f),
        Height = height,
        BackColor = Theme.Surface,
        Margin = new Padding(0, top, FieldLabelGap, 0)
    };

    /// <summary>Makes <paramref name="label"/> one line, <paramref name="height"/> tall, text centred vertically.</summary>
    public static void CentreOn(Label label, int top, int height)
    {
        var width = TextRenderer.MeasureText(label.Text, label.Font).Width;
        label.AutoSize = false;
        label.Size = new Size(width, height);
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = new Padding(label.Margin.Left, top, label.Margin.Right, 0);
    }
}
