using System.ComponentModel;

namespace Rigshift.Settings;

/// <summary>
/// A text tab strip that replaces the stock <see cref="TabControl"/>: the captions are
/// painted in <see cref="Theme"/>, the selected tab has an accent underline, and a border
/// rule closes the strip underneath. The form hosts the active editor elsewhere, so this
/// control only reports which tab is selected.
/// </summary>
public class TabStrip : Control
{
    private const int BaseGap = 24;
    private const int LeftPad = 16;
    private const int RuleClearance = 3;

    private readonly string[] _titles;
    private int _selectedIndex;

    public TabStrip(params string[] titles)
    {
        _titles = titles ?? throw new ArgumentNullException(nameof(titles));
        _selectedIndex = 0;

        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        BackColor = Theme.Bg;
        Font = Theme.UiSemibold;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PageTabList;
        Height = 22;
    }

    private float DpiScale => DeviceDpi / 96f;

    public IReadOnlyList<string> Titles => _titles;

    [DefaultValue(0)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var index = Math.Clamp(value, 0, _titles.Length - 1);
            if (_selectedIndex == index)
                return;
            _selectedIndex = index;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SelectedIndexChanged;

    public int TabCount => _titles.Length;

    /// <summary>The text bounds of one tab, following the paint layout.</summary>
    public Rectangle TabBounds(int index)
    {
        var left = ThemePainter.Px(LeftPad, DpiScale);
        for (var i = 0; i < _titles.Length; i++)
        {
            var text = _titles[i];
            var size = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.SingleLine);
            if (i == index)
                return new Rectangle(left, 0, size.Width, ClientSize.Height);
            left += size.Width + ThemePainter.Px(BaseGap, DpiScale);
        }
        return Rectangle.Empty;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var scale = DpiScale;
        using (var back = new SolidBrush(BackColor))
            g.FillRectangle(back, ClientRectangle);

        var ruleY = ClientRectangle.Bottom - ThemePainter.Px(1, scale);
        ThemePainter.DrawRule(g, 0, ruleY, ClientRectangle.Width, scale);

        var left = ThemePainter.Px(LeftPad, scale);
        for (var i = 0; i < _titles.Length; i++)
        {
            var title = _titles[i];
            var color = i == _selectedIndex ? Theme.Text : Theme.Muted;
            var bounds = new Rectangle(left, 0, Math.Max(0, ClientSize.Width - left), ClientSize.Height);
            TextRenderer.DrawText(g, title, Font, bounds, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            if (i == _selectedIndex && Enabled)
            {
                var textWidth = TextRenderer.MeasureText(title, Font, Size.Empty, TextFormatFlags.SingleLine).Width;
                ThemePainter.DrawUnderline(g, left, textWidth,
                    ruleY - ThemePainter.Px(RuleClearance, scale), scale);
            }

            left += MeasureTitle(title) + ThemePainter.Px(BaseGap, scale);
        }
    }

    private int MeasureTitle(string title) =>
        TextRenderer.MeasureText(title, Font, Size.Empty, TextFormatFlags.SingleLine).Width;

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        for (var i = 0; i < _titles.Length; i++)
        {
            if (TabBounds(i).Contains(e.Location) && e.X >= 0)
            {
                SelectedIndex = i;
                return;
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int target;
        if (e.Control && e.KeyCode == Keys.Tab)
        {
            target = e.Shift
                ? (_selectedIndex - 1 + _titles.Length) % _titles.Length
                : (_selectedIndex + 1) % _titles.Length;
            e.Handled = true;
        }
        else
        {
            target = e.KeyCode switch
            {
                Keys.Left => _selectedIndex - 1,
                Keys.Right => _selectedIndex + 1,
                _ => -1
            };
        }

        bool handled = e.KeyCode is Keys.Left or Keys.Right or Keys.Tab;
        if (target >= 0 && target < _titles.Length)
            SelectedIndex = target;

        if (!handled)
            base.OnKeyDown(e);
    }
}
