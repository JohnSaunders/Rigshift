using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Rigshift.Settings;

/// <summary>
/// Read-only wrapped text with the same thin scroll thumb as <see cref="ThemedListBox"/>
/// in place of the native scroll bar. The text stays a <see cref="TextBox"/>, so it can
/// be selected and copied; the wheel, keyboard and thumb scroll it by lines.
/// </summary>
public partial class ThemedTextView : Control
{
    private const int ThumbWidth = 4;
    private const int ThumbGap = 2;
    private const int StripWidth = 10;
    private const int ThumbMinHeight = 18;

    private readonly ViewBox _box;
    private Rectangle _paintedThumb;
    private bool _thumbHot;
    private int _thumbGrab = -1;
    private int _wheelRemainder;

    public ThemedTextView()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        Font = Theme.UiFont;

        _box = new ViewBox(this)
        {
            ReadOnly = true,
            Multiline = true,
            WordWrap = true,
            ScrollBars = ScrollBars.None,
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            ForeColor = ForeColor,
            Font = Font
        };
        _box.TextChanged += (_, _) => RefreshThumb();
        _box.Resize += (_, _) => RefreshThumb();
        Padding = new Padding(0, 0, ThemePainter.Px(StripWidth, DeviceDpi / 96f), 0);
        Controls.Add(_box);
    }

    /// <summary>
    /// A plain control docks children into its whole client area, so keep the
    /// right-hand strip free for the thumb by honouring Padding here.
    /// </summary>
    public override Rectangle DisplayRectangle
    {
        get
        {
            var r = ClientRectangle;
            return new Rectangle(
                r.Left + Padding.Left,
                r.Top + Padding.Top,
                Math.Max(0, r.Width - Padding.Horizontal),
                Math.Max(0, r.Height - Padding.Vertical));
        }
    }

    /// <summary>The text box inside, for focus and accessibility.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Inner => _box;

    [AllowNull]
    public override string Text
    {
        get => _box.Text;
        set => _box.Text = value ?? "";
    }

    /// <summary>How many wrapped lines the text takes.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int LineCount => _box.TextLength == 0 ? 0 : _box.GetLineFromCharIndex(_box.TextLength) + 1;

    /// <summary>
    /// How many lines are in view, counting a partly shown last line, as the
    /// edit control does when it limits scrolling.
    /// </summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int VisibleLineCount
    {
        get
        {
            var line = Math.Max(1, _box.Font.Height);
            return Math.Max(1, (_box.ClientSize.Height + line - 1) / line);
        }
    }

    /// <summary>The first line in view.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int FirstVisibleLine => _box.IsHandleCreated ? (int)SendMessage(_box.Handle, EM_GETFIRSTVISIBLELINE, 0, 0) : 0;

    /// <summary>True when the text runs past the box, so the thumb shows.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowsScrollThumb => _box.IsHandleCreated && LineCount > VisibleLineCount;

    /// <summary>The scroll thumb in client coordinates; empty when all the text fits.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle ScrollThumbBounds
    {
        get
        {
            if (!ShowsScrollThumb)
                return Rectangle.Empty;
            var scale = DeviceDpi / 96f;
            var width = ThemePainter.Px(ThumbWidth, scale);
            var gap = ThemePainter.Px(ThumbGap, scale);
            var track = ClientSize.Height - 2 * gap;
            var lines = LineCount;
            var visible = VisibleLineCount;
            var length = Math.Min(track, Math.Max(ThemePainter.Px(ThumbMinHeight, scale), track * visible / lines));
            var maxFirst = Math.Max(1, lines - visible);
            var top = gap + (track - length) * Math.Clamp(FirstVisibleLine, 0, maxFirst) / maxFirst;
            return new Rectangle(ClientSize.Width - gap - width, top, width, length);
        }
    }

    /// <summary>Scrolls so <paramref name="line"/> is the first line in view, within the text.</summary>
    public void ScrollToLine(int line)
    {
        if (!_box.IsHandleCreated)
            return;
        var target = Math.Clamp(line, 0, Math.Max(0, LineCount - VisibleLineCount));
        var delta = target - FirstVisibleLine;
        if (delta != 0)
            SendMessage(_box.Handle, EM_LINESCROLL, 0, delta);
        RefreshThumb();
    }

    /// <summary>Scrolls as dragging the thumb to <paramref name="pointerY"/> does.</summary>
    public void DragThumbTo(int pointerY)
    {
        var thumb = ScrollThumbBounds;
        if (thumb.IsEmpty)
            return;
        var gap = ThemePainter.Px(ThumbGap, DeviceDpi / 96f);
        var travel = ClientSize.Height - 2 * gap - thumb.Height;
        var maxFirst = LineCount - VisibleLineCount;
        if (travel <= 0 || maxFirst <= 0)
            return;
        var offset = pointerY - Math.Max(0, _thumbGrab) - gap;
        ScrollToLine((int)Math.Round((double)offset * maxFirst / travel));
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        // The base constructor sets the colours before the box exists.
        if (_box is not null)
            _box.BackColor = BackColor;
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        if (_box is not null)
            _box.ForeColor = ForeColor;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_box is not null)
            _box.Font = Font;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (var back = new SolidBrush(BackColor))
            e.Graphics.FillRectangle(back, ClientRectangle);
        var thumb = ScrollThumbBounds;
        _paintedThumb = thumb;
        if (!thumb.IsEmpty)
            ThemePainter.FillCapsule(e.Graphics, thumb, _thumbGrab >= 0 || _thumbHot ? Theme.Muted : Theme.InputBorder);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        var thumb = ScrollThumbBounds;
        if (e.Button != MouseButtons.Left || thumb.IsEmpty)
            return;
        if (e.Y >= thumb.Top && e.Y < thumb.Bottom)
        {
            _thumbGrab = e.Y - thumb.Top;
            Capture = true;
            Invalidate();
        }
        else
        {
            var page = Math.Max(1, VisibleLineCount - 1);
            ScrollToLine(FirstVisibleLine + (e.Y < thumb.Top ? -page : page));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_thumbGrab >= 0)
        {
            DragThumbTo(e.Y);
            return;
        }
        var hot = !ScrollThumbBounds.IsEmpty;
        if (hot != _thumbHot)
        {
            _thumbHot = hot;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_thumbGrab < 0)
            return;
        _thumbGrab = -1;
        Capture = false;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_thumbHot && _thumbGrab < 0)
        {
            _thumbHot = false;
            Invalidate();
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Wheel(e.Delta);
    }

    // The edit control leaves the wheel alone without its scroll bar, so the wheel
    // scrolls the lines here, by the system's lines per notch.
    private bool Wheel(int delta)
    {
        if (!ShowsScrollThumb)
            return false;
        _wheelRemainder += delta;
        var notches = _wheelRemainder / 120;
        _wheelRemainder -= notches * 120;
        if (notches != 0)
        {
            var lines = SystemInformation.MouseWheelScrollLines;
            var perNotch = lines <= 0 ? Math.Max(1, VisibleLineCount - 1) : lines;
            ScrollToLine(FirstVisibleLine - notches * perNotch);
        }
        return true;
    }

    private void RefreshThumb()
    {
        if (IsHandleCreated && ScrollThumbBounds != _paintedThumb)
            Invalidate();
    }

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_VSCROLL = 0x0115;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int EM_LINESCROLL = 0x00B6;
    private const int EM_SCROLLCARET = 0x00B7;
    private const int EM_GETFIRSTVISIBLELINE = 0x00CE;

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial IntPtr SendMessage(IntPtr hWnd, int msg, nint wParam, nint lParam);

    /// <summary>The text box: it hands the wheel to the view and tells it when it scrolled.</summary>
    private sealed class ViewBox : TextBox
    {
        private readonly ThemedTextView _owner;

        public ViewBox(ThemedTextView owner) => _owner = owner;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL && _owner.Wheel(unchecked((short)((long)m.WParam >> 16))))
                return;

            base.WndProc(ref m);
            switch (m.Msg)
            {
                case WM_KEYDOWN:
                case WM_VSCROLL:
                case WM_MOUSEMOVE:
                case WM_LBUTTONUP:
                case EM_LINESCROLL:
                case EM_SCROLLCARET:
                    _owner.RefreshThumb();
                    break;
            }
        }
    }
}
