using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Rigshift.Settings;

/// <summary>
/// A button painted from <see cref="Theme"/>: outlined by default, a solid accent fill
/// when <see cref="Primary"/>. It stays a <see cref="Button"/>, so click, mnemonics,
/// keyboard, dialog results and accessibility behave as before; only painting changes.
/// </summary>
public class ThemedButton : Button
{
    private const int PlusArm = 4;
    private const int PlusGap = 6;

    private bool _primary;
    private bool _showPlus;
    private bool _mouseDown;
    private bool _spaceDown;

    public ThemedButton()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        ApplyLook();
        Height = Theme.ButtonHeight;
    }

    /// <summary>True for the solid accent button (Save); false for an outlined one.</summary>
    [DefaultValue(false)]
    public bool Primary
    {
        get => _primary;
        set
        {
            if (_primary == value)
                return;
            _primary = value;
            ApplyLook();
            Invalidate();
        }
    }

    /// <summary>
    /// True to draw a plus sign before the text (an "add" action). The text, and so the
    /// accessible name, stays as set.
    /// </summary>
    [DefaultValue(false)]
    public bool ShowPlus
    {
        get => _showPlus;
        set
        {
            if (_showPlus == value)
                return;
            _showPlus = value;
            Invalidate();
        }
    }

    /// <summary>True while the mouse is over the button.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsHot { get; private set; }

    /// <summary>True while the left mouse button or the Space key holds the button down.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsPressed { get; private set; }

    private float DpiScale => DeviceDpi / 96f;

    private void ApplyLook()
    {
        Font = _primary ? Theme.UiSemibold : Theme.UiFont;
        BackColor = _primary ? Theme.Accent : Theme.Surface;
        ForeColor = _primary ? Theme.AccentContrast : Theme.Text;
    }

    private void SetHot(bool value)
    {
        if (IsHot == value)
            return;
        IsHot = value;
        Invalidate();
    }

    // Pressed while either the left mouse button or Space holds the button down, so
    // releasing one input does not show the face released while the other still holds it.
    private void SetPressed(bool? mouse = null, bool? space = null)
    {
        if (mouse is { } m)
            _mouseDown = m;
        if (space is { } s)
            _spaceDown = s;
        var value = _mouseDown || _spaceDown;
        if (IsPressed == value)
            return;
        IsPressed = value;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        SetHot(true);
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        SetHot(false);
        SetPressed(mouse: false, space: false);
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
            SetPressed(mouse: true);
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
            SetPressed(mouse: false);
        base.OnMouseUp(mevent);
    }

    protected override void OnKeyDown(KeyEventArgs kevent)
    {
        if (kevent.KeyCode == Keys.Space)
            SetPressed(space: true);
        base.OnKeyDown(kevent);
    }

    protected override void OnKeyUp(KeyEventArgs kevent)
    {
        if (kevent.KeyCode == Keys.Space)
            SetPressed(space: false);
        base.OnKeyUp(kevent);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        SetPressed(mouse: false, space: false);
        base.OnLostFocus(e);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        var scale = DpiScale;
        var text = ThemedText.StripMnemonic(Text);
        ThemePainter.DrawButton(
            g, ClientRectangle, _showPlus ? "" : text, Font,
            _primary, IsHot, IsPressed, Enabled, scale);

        if (_showPlus)
            PaintPlusAndText(g, text, scale);

        if (Focused && ShowFocusCues)
        {
            var inset = ThemePainter.Px(2, scale);
            var cue = Rectangle.Inflate(ClientRectangle, -inset, -inset);
            ThemePainter.FillFrame(g, cue, Color.Transparent, _primary ? Theme.AccentContrast : Theme.Accent, scale);
        }
    }

    // The plus and the text are centred together as one group.
    private void PaintPlusAndText(Graphics g, string text, float scale)
    {
        var color = !Enabled ? Theme.Muted : _primary ? Theme.AccentContrast : Theme.Text;
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        var textSize = TextRenderer.MeasureText(g, text, Font, Size.Empty, flags);
        var glyph = 2 * ThemePainter.Px(PlusArm, scale) + 1;
        var gap = text.Length == 0 ? 0 : ThemePainter.Px(PlusGap, scale);
        var groupWidth = glyph + gap + textSize.Width;
        var left = (ClientSize.Width - groupWidth) / 2;

        ThemePainter.DrawPlus(g, new Rectangle(left, 0, glyph, ClientSize.Height), color, PlusArm, scale);
        if (text.Length == 0)
            return;
        var textBounds = new Rectangle(left + glyph + gap, 0, Math.Max(0, ClientSize.Width - left - glyph - gap), ClientSize.Height);
        TextRenderer.DrawText(g, text, Font, textBounds, color, flags | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var scale = DpiScale;
        var text = TextRenderer.MeasureText(ThemedText.StripMnemonic(Text), Font);
        var glyph = _showPlus ? 2 * ThemePainter.Px(PlusArm, scale) + 1 + ThemePainter.Px(PlusGap, scale) : 0;
        var width = Math.Max(text.Width + glyph + 2 * ThemePainter.Px(16, scale), ThemePainter.Px(64, scale));
        return new Size(width, ThemePainter.Px(Theme.ButtonHeight, scale));
    }
}

/// <summary>
/// A checkbox painted from <see cref="Theme"/>: an outlined box, or an accent box with
/// a dark tick, then its text. It stays a <see cref="CheckBox"/>, so clicking, Space,
/// mnemonics, <see cref="CheckBox.CheckedChanged"/> and accessibility behave as before.
/// </summary>
public class ThemedCheckBox : CheckBox
{
    private const int BoxSize = 16;
    private const int TextGap = 8;

    public ThemedCheckBox()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        Font = Theme.UiFont;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        AutoSize = true;
    }

    private float DpiScale => DeviceDpi / 96f;

    /// <summary>The check box square in client coordinates.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle BoxBounds
    {
        get
        {
            var size = ThemePainter.Px(BoxSize, DpiScale);
            return new Rectangle(0, (ClientSize.Height - size) / 2, size, size);
        }
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        var scale = DpiScale;
        using (var back = new SolidBrush(BackColor))
            g.FillRectangle(back, ClientRectangle);

        var box = BoxBounds;
        // Indeterminate is never used by the app; it paints as checked.
        ThemePainter.DrawCheck(g, box, CheckState != CheckState.Unchecked, Enabled, scale);

        if (Focused && ShowFocusCues)
        {
            var outset = ThemePainter.Px(2, scale);
            ThemePainter.FillFrame(g, Rectangle.Inflate(box, outset, outset), Color.Transparent, Theme.Accent, scale);
        }

        var left = box.Right + ThemePainter.Px(TextGap, scale);
        var textBounds = new Rectangle(left, 0, Math.Max(0, ClientSize.Width - left), ClientSize.Height);
        var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
        if (!ShowKeyboardCues)
            flags |= TextFormatFlags.HidePrefix;
        TextRenderer.DrawText(g, Text, Font, textBounds, Enabled ? Theme.Text : Theme.Muted, flags);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var scale = DpiScale;
        // TextRenderer treats '&' as the mnemonic marker unless NoPrefix is set, so the
        // marker takes no width here, matching what OnPaint draws.
        var text = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine);
        var box = ThemePainter.Px(BoxSize, scale);
        var pad = ThemePainter.Px(4, scale);
        var width = box + ThemePainter.Px(TextGap, scale) + text.Width + 1;
        var height = Math.Max(box + pad, text.Height + pad);
        return new Size(width, height);
    }
}

/// <summary>
/// A surface box with a one-logical-pixel border frame, padded so a docked child
/// (a <see cref="ThemedListBox"/>) sits inside the frame.
/// </summary>
public class ThemedFrame : Panel
{
    public ThemedFrame()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        BackColor = Theme.Surface;
        Padding = new Padding(ThemePainter.Px(4, DpiScale));
    }

    private float DpiScale => DeviceDpi / 96f;

    protected override void OnPaint(PaintEventArgs e)
    {
        ThemePainter.FillFrame(e.Graphics, ClientRectangle, Theme.Surface, Theme.Border, DpiScale);
    }
}

/// <summary>
/// An owner-drawn list on the surface: rows <see cref="Theme.RowHeight"/> tall, the
/// selected row with the selected fill and an accent bar, an optional colour pip per
/// row, and a muted <see cref="EmptyText"/> when the list has no items. Selection,
/// keyboard and accessibility stay the stock <see cref="ListBox"/>.
/// </summary>
public class ThemedListBox : ListBox
{
    private const int WM_PAINT = 0x000F;
    private const int RowInset = 3;
    private const int PipCentre = 13;
    private const int TextWithPip = 29;
    private const int TextWithoutPip = 11;
    private const int GlyphSize = 20;
    private const int GlyphInset = 6;
    private const int GlyphInsetBesideThumb = 10;
    private const int ThumbWidth = 4;
    private const int ThumbGap = 2;
    private const int ThumbHitWidth = 8;
    private const int ThumbMinHeight = 24;

    private const int WM_SIZE = 0x0005;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_VSCROLL = 0x0115;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_CAPTURECHANGED = 0x0215;
    private const int LB_ADDSTRING = 0x0180;
    private const int LB_INSERTSTRING = 0x0181;
    private const int LB_DELETESTRING = 0x0182;
    private const int LB_RESETCONTENT = 0x0184;
    private const int LB_SETCURSEL = 0x0186;
    private const int LB_SETTOPINDEX = 0x0197;
    private const int WS_VSCROLL = 0x00200000;

    private string _emptyText = "";
    private Func<int, Color?>? _pipColor;
    private bool _showRemoveGlyph;
    private int _hoverIndex = -1;
    private bool _hoverGlyph;
    private int _pressedGlyph = -1;

    // The scroll thumb: where it was last painted, whether the pointer is over its strip,
    // and, while it is dragged, how far below the thumb's top it was grabbed.
    private Rectangle _paintedThumb;
    private bool _thumbHot;
    private int _thumbGrab = -1;
    private bool _checkingThumb;
    private int _wheelRemainder;

    public ThemedListBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        BorderStyle = BorderStyle.None;
        IntegralHeight = false;
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        Font = Theme.UiFont;
        ItemHeight = ThemePainter.Px(Theme.RowHeight, DpiScale);
    }

    private float DpiScale => DeviceDpi / 96f;

    /// <summary>
    /// The colour of each row's pip; a null result draws an empty ring. When this is
    /// null no pip column is drawn.
    /// </summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<int, Color?>? PipColor
    {
        get => _pipColor;
        set
        {
            _pipColor = value;
            Invalidate();
        }
    }

    /// <summary>
    /// True to draw a remove cross at the right of every row. Clicking the cross raises
    /// <see cref="RemoveClicked"/> with the row index; the Delete key is left to the owner.
    /// </summary>
    [DefaultValue(false)]
    public bool ShowRemoveGlyph
    {
        get => _showRemoveGlyph;
        set
        {
            _showRemoveGlyph = value;
            Invalidate();
        }
    }

    /// <summary>
    /// The window has no native scroll bar: the list scrolls by wheel, keyboard and
    /// selection as before, and paints its own thin thumb at the right edge instead.
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style &= ~WS_VSCROLL;
            return cp;
        }
    }

    /// <summary>How many whole rows fit in the list.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int VisibleRowCount => ItemHeight <= 0 ? 0 : Math.Max(1, ClientSize.Height / ItemHeight);

    /// <summary>True when there are more rows than fit, so the scroll thumb shows.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowsScrollThumb => Items.Count > VisibleRowCount && ClientSize.Height > 0;

    /// <summary>
    /// The scroll thumb in client coordinates: a narrow capsule at the right edge whose
    /// length is the visible share of the rows and whose position follows the top row.
    /// Empty when every row fits.
    /// </summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle ScrollThumbBounds
    {
        get
        {
            if (!ShowsScrollThumb)
                return Rectangle.Empty;

            var scale = DpiScale;
            var width = ThemePainter.Px(ThumbWidth, scale);
            var gap = ThemePainter.Px(ThumbGap, scale);
            var track = ClientSize.Height - 2 * gap;
            var length = Math.Max(ThemePainter.Px(ThumbMinHeight, scale), track * VisibleRowCount / Items.Count);
            length = Math.Min(length, track);
            var maxTop = Math.Max(1, Items.Count - VisibleRowCount);
            var top = gap + (track - length) * Math.Clamp(TopIndex, 0, maxTop) / maxTop;
            return new Rectangle(ClientSize.Width - gap - width, top, width, length);
        }
    }

    /// <summary>The strip at the right edge that takes the pointer for the scroll thumb.</summary>
    private Rectangle ThumbStrip => ShowsScrollThumb
        ? new Rectangle(ClientSize.Width - ThemePainter.Px(ThumbHitWidth, DpiScale), 0, ThemePainter.Px(ThumbHitWidth, DpiScale), ClientSize.Height)
        : Rectangle.Empty;

    /// <summary>Paints the scroll thumb, brighter while the pointer is on it or it is dragged.</summary>
    public void PaintScrollThumb(Graphics g)
    {
        var thumb = ScrollThumbBounds;
        _paintedThumb = thumb;
        if (thumb.IsEmpty)
            return;
        var color = _thumbGrab >= 0 || _thumbHot ? Theme.Muted : Theme.InputBorder;
        ThemePainter.FillCapsule(g, thumb, Enabled ? color : Theme.Border);
    }

    /// <summary>Raised with the row index when that row's remove cross is clicked.</summary>
    public event EventHandler<int>? RemoveClicked;

    /// <summary>The remove cross of the row in <paramref name="row"/>, in client coordinates.</summary>
    public Rectangle RemoveGlyphBounds(Rectangle row)
    {
        var scale = DpiScale;
        var size = ThemePainter.Px(GlyphSize, scale);
        var right = row.Right - ThemePainter.Px(ShowsScrollThumb ? GlyphInsetBesideThumb : GlyphInset, scale);
        return new Rectangle(right - size, row.Top + (row.Height - size) / 2, size, size);
    }

    /// <summary>Muted text drawn centred when the list has no items.</summary>
    [DefaultValue("")]
    public string EmptyText
    {
        get => _emptyText;
        set
        {
            _emptyText = value ?? "";
            Invalidate();
        }
    }

    /// <summary>Paints one row into <paramref name="bounds"/>. An index outside the items draws nothing.</summary>
    public void PaintRow(Graphics g, Rectangle bounds, int index, bool selected)
    {
        if (index < 0 || index >= Items.Count)
            return;

        var scale = DpiScale;
        using (var surface = new SolidBrush(BackColor))
            g.FillRectangle(surface, bounds);

        if (selected)
        {
            var inset = ThemePainter.Px(RowInset, scale);
            ThemePainter.DrawSelectedRow(g, Rectangle.Inflate(bounds, -inset, 0), scale);
        }

        var centreY = bounds.Top + bounds.Height / 2;
        int textLeft;
        if (_pipColor is not null)
        {
            ThemePainter.DrawPip(g, new Point(bounds.Left + ThemePainter.Px(PipCentre, scale), centreY), _pipColor(index), scale);
            textLeft = ThemePainter.Px(TextWithPip, scale);
        }
        else
        {
            textLeft = ThemePainter.Px(TextWithoutPip, scale);
        }

        var rightReserve = ThemePainter.Px(ShowsScrollThumb ? ThumbHitWidth : RowInset, scale);
        if (_showRemoveGlyph)
        {
            var glyph = RemoveGlyphBounds(bounds);
            rightReserve = bounds.Right - glyph.Left;
            var hot = Enabled && index == _hoverIndex && _hoverGlyph;
            var rowHot = Enabled && (index == _hoverIndex || selected);
            if (hot)
                ThemePainter.FillFrame(g, glyph, Theme.Blend(Theme.Danger, BackColor, 0.14), Theme.Blend(Theme.Danger, BackColor, 0.14), scale);
            ThemePainter.DrawCross(g, glyph, hot ? Theme.Danger : rowHot ? Theme.Muted : Theme.Border, scale);
        }

        var textBounds = new Rectangle(bounds.Left + textLeft, bounds.Top, Math.Max(0, bounds.Width - textLeft - rightReserve), bounds.Height);
        TextRenderer.DrawText(
            g, GetItemText(Items[index]), Font, textBounds, Enabled ? Theme.Text : Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    /// <summary>Paints the empty state: the surface, then <see cref="EmptyText"/> centred in muted.</summary>
    public void PaintEmpty(Graphics g, Rectangle client)
    {
        using (var surface = new SolidBrush(BackColor))
            g.FillRectangle(surface, client);
        if (_emptyText.Length == 0)
            return;
        TextRenderer.DrawText(
            g, _emptyText, Font, client, Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        PaintRow(e.Graphics, e.Bounds, e.Index, (e.State & DrawItemState.Selected) != 0);
    }

    protected override void WndProc(ref Message m)
    {
        if (HandleThumbMouse(ref m))
            return;

        base.WndProc(ref m);
        switch (m.Msg)
        {
            case WM_PAINT when IsHandleCreated:
                using (var g = CreateGraphics())
                {
                    if (Items.Count == 0)
                        PaintEmpty(g, ClientRectangle);
                    else
                        PaintScrollThumb(g);
                }
                break;
            case WM_SIZE:
            case WM_KEYDOWN:
            case WM_VSCROLL:
            case WM_MOUSEMOVE:
            case WM_LBUTTONDOWN:
            case WM_LBUTTONUP:
            case WM_MOUSEWHEEL:
            case LB_ADDSTRING:
            case LB_INSERTSTRING:
            case LB_DELETESTRING:
            case LB_RESETCONTENT:
            case LB_SETCURSEL:
            case LB_SETTOPINDEX:
                RefreshThumbIfMoved();
                break;
        }
    }

    // The list scrolls its pixels, thumb and all, so a moved or resized thumb repaints
    // the strip it left and the one it now takes.
    private void RefreshThumbIfMoved()
    {
        if (_checkingThumb || !IsHandleCreated)
            return;
        _checkingThumb = true;
        try
        {
            var thumb = ScrollThumbBounds;
            if (thumb == _paintedThumb)
                return;
            var scale = DpiScale;
            var strip = new Rectangle(ClientSize.Width - ThemePainter.Px(ThumbHitWidth, scale), 0, ThemePainter.Px(ThumbHitWidth, scale), ClientSize.Height);
            Invalidate(strip);
            if (!_paintedThumb.IsEmpty)
                Invalidate(Rectangle.Union(_paintedThumb, strip));
        }
        finally
        {
            _checkingThumb = false;
        }
    }

    // Pointer input on the thumb strip drives the thumb instead of selecting a row:
    // drag the thumb, or click above or below it to move a page.
    private bool HandleThumbMouse(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_LBUTTONDOWN:
            case WM_LBUTTONDBLCLK:
            {
                var point = PointFromLParam(m.LParam);
                if (!Enabled || !ThumbStrip.Contains(point))
                    return false;
                var thumb = ScrollThumbBounds;
                if (point.Y >= thumb.Top && point.Y < thumb.Bottom)
                {
                    _thumbGrab = point.Y - thumb.Top;
                    Capture = true;
                    InvalidateThumb();
                }
                else
                {
                    var page = Math.Max(1, VisibleRowCount - 1);
                    ScrollTo(TopIndex + (point.Y < thumb.Top ? -page : page));
                }
                return true;
            }
            case WM_MOUSEMOVE:
            {
                var point = PointFromLParam(m.LParam);
                if (_thumbGrab >= 0)
                {
                    DragThumbTo(point.Y);
                    return true;
                }
                SetThumbHot(ThumbStrip.Contains(point));
                return false;
            }
            case WM_MOUSEWHEEL:
            {
                // Without the native scroll bar the list leaves the wheel alone, so the
                // wheel scrolls the rows here, by the system's lines per notch.
                if (!ShowsScrollThumb)
                    return false;
                _wheelRemainder += unchecked((short)((long)m.WParam >> 16));
                var notches = _wheelRemainder / 120;
                _wheelRemainder -= notches * 120;
                if (notches != 0)
                {
                    var lines = SystemInformation.MouseWheelScrollLines;
                    var perNotch = lines <= 0 ? Math.Max(1, VisibleRowCount - 1) : lines;
                    ScrollTo(TopIndex - notches * perNotch);
                }
                return true;
            }
            case WM_LBUTTONUP when _thumbGrab >= 0:
                EndThumbDrag();
                return true;
            case WM_CAPTURECHANGED when _thumbGrab >= 0:
                EndThumbDrag();
                return false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Scrolls as dragging the thumb to <paramref name="pointerY"/> does: the point where
    /// the thumb was grabbed (its top when it is not being dragged) follows the pointer.
    /// </summary>
    public void DragThumbTo(int pointerY)
    {
        var grab = Math.Max(0, _thumbGrab);
        var thumb = ScrollThumbBounds;
        if (thumb.IsEmpty)
            return;
        var gap = ThemePainter.Px(ThumbGap, DpiScale);
        var travel = ClientSize.Height - 2 * gap - thumb.Height;
        var maxTop = Items.Count - VisibleRowCount;
        if (travel <= 0 || maxTop <= 0)
            return;
        var offset = pointerY - grab - gap;
        ScrollTo((int)Math.Round((double)offset * maxTop / travel));
    }

    private void ScrollTo(int top)
    {
        var maxTop = Math.Max(0, Items.Count - VisibleRowCount);
        top = Math.Clamp(top, 0, maxTop);
        if (top != TopIndex)
            TopIndex = top;
        RefreshThumbIfMoved();
    }

    private void EndThumbDrag()
    {
        _thumbGrab = -1;
        if (Capture)
            Capture = false;
        InvalidateThumb();
    }

    private void SetThumbHot(bool hot)
    {
        if (hot == _thumbHot)
            return;
        _thumbHot = hot;
        InvalidateThumb();
    }

    private void InvalidateThumb()
    {
        if (IsHandleCreated && !_paintedThumb.IsEmpty)
            Invalidate(Rectangle.Inflate(_paintedThumb, 1, 1));
    }

    private static Point PointFromLParam(IntPtr lParam)
    {
        var value = unchecked((int)(long)lParam);
        return new Point(unchecked((short)(value & 0xFFFF)), unchecked((short)((value >> 16) & 0xFFFF)));
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Items.Count == 0)
            Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_showRemoveGlyph)
            return;
        var index = IndexFromPoint(e.Location);
        if (index == NoMatches)
            index = -1;
        var onGlyph = index >= 0 && RemoveGlyphBounds(GetItemRectangle(index)).Contains(e.Location);
        SetHover(index, onGlyph);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _pressedGlyph = -1;
        SetHover(-1, false);
        SetThumbHot(false);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _pressedGlyph = e.Button == MouseButtons.Left ? GlyphIndexAt(e.Location) : -1;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        var pressed = _pressedGlyph;
        _pressedGlyph = -1;
        if (pressed >= 0 && e.Button == MouseButtons.Left && GlyphIndexAt(e.Location) == pressed)
        {
            SetHover(-1, false);
            RemoveClicked?.Invoke(this, pressed);
        }
    }

    // The row whose remove cross is under the point, or -1.
    private int GlyphIndexAt(Point point)
    {
        if (!_showRemoveGlyph || !Enabled)
            return -1;
        var index = IndexFromPoint(point);
        if (index == NoMatches || index < 0 || index >= Items.Count)
            return -1;
        return RemoveGlyphBounds(GetItemRectangle(index)).Contains(point) ? index : -1;
    }

    private void SetHover(int index, bool onGlyph)
    {
        if (index == _hoverIndex && onGlyph == _hoverGlyph)
            return;
        InvalidateRow(_hoverIndex);
        _hoverIndex = index;
        _hoverGlyph = onGlyph;
        InvalidateRow(_hoverIndex);
        Cursor = onGlyph ? Cursors.Hand : Cursors.Default;
    }

    private void InvalidateRow(int index)
    {
        if (index >= 0 && index < Items.Count && IsHandleCreated)
            Invalidate(GetItemRectangle(index));
    }
}

/// <summary>
/// A text field: a borderless <see cref="TextBox"/> inside a painted field frame
/// whose bottom edge turns accent while the text box has focus.
/// </summary>
public class ThemedTextBox : Control
{
    private const int TextInset = 8;
    private bool _mono;

    public ThemedTextBox()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);
        BackColor = Theme.Surface;

        Inner = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            Font = Theme.UiFont
        };
        Inner.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
        Inner.GotFocus += (_, _) => Invalidate();
        Inner.LostFocus += (_, _) => Invalidate();
        Controls.Add(Inner);

        Size = new Size(240, ThemePainter.Px(Theme.FieldHeight, DpiScale));
        LayoutInner();
    }

    /// <summary>The hosted text box. Read and write its properties for anything not surfaced here.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Inner { get; }

    private float DpiScale => DeviceDpi / 96f;

    [AllowNull]
    public override string Text
    {
        get => Inner.Text;
        set => Inner.Text = value ?? "";
    }

    /// <summary>True for Consolas (hex values, paths); false for the UI font.</summary>
    [DefaultValue(false)]
    public bool Mono
    {
        get => _mono;
        set
        {
            _mono = value;
            Inner.Font = value ? Theme.MonoFont : Theme.UiFont;
            LayoutInner();
        }
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        LayoutInner();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutInner();
    }

    private void LayoutInner()
    {
        var inset = ThemePainter.Px(TextInset, DpiScale);
        var width = Math.Max(0, ClientSize.Width - 2 * inset);
        var top = Math.Max(0, (ClientSize.Height - Inner.Height) / 2);
        Inner.SetBounds(inset, top, width, Inner.Height);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Inner.Focus();
        base.OnMouseDown(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        // A disabled field greys its text: the inner text box must follow the outer
        // control, or it would stay interactive and render in Text instead of Muted.
        Inner.Enabled = Enabled;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        ThemePainter.DrawField(e.Graphics, ClientRectangle, Inner.Focused, Enabled, DpiScale);
    }
}

/// <summary>Text helpers shared by the themed controls.</summary>
public static class ThemedText
{
    /// <summary>
    /// Removes mnemonic markers the way Windows shows them: a single <c>&amp;</c> is
    /// dropped and <c>&amp;&amp;</c> becomes one <c>&amp;</c>.
    /// </summary>
    public static string StripMnemonic(string? text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('&'))
            return text ?? string.Empty;

        var result = new System.Text.StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '&')
            {
                if (i + 1 < text.Length && text[i + 1] == '&')
                {
                    result.Append('&');
                    i++;
                }
                continue;
            }
            result.Append(text[i]);
        }
        return result.ToString();
    }
}
