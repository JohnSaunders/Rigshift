namespace Rigshift.Settings;

/// <summary>
/// A drop-down menu painted from <see cref="Theme"/>: a surface background with a border,
/// the hovered item in the selected fill, and item text in <see cref="Theme.Text"/>
/// (<see cref="Theme.Muted"/> when disabled). Keyboard and accessibility stay the stock
/// <see cref="ContextMenuStrip"/>.
/// </summary>
public class ThemedMenu : ContextMenuStrip
{
    public ThemedMenu()
    {
        Renderer = new ThemedMenuRenderer();
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        Font = Theme.UiFont;
        ShowImageMargin = false;
        ShowCheckMargin = false;
    }

    private sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemedMenuRenderer() : base(new ThemedMenuColors())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Muted;
            base.OnRenderItemText(e);
        }
    }

    private sealed class ThemedMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Surface;
        public override Color MenuBorder => Theme.Border;
        public override Color MenuItemBorder => Theme.SelectedFill;
        public override Color MenuItemSelected => Theme.SelectedFill;
        public override Color MenuItemSelectedGradientBegin => Theme.SelectedFill;
        public override Color MenuItemSelectedGradientEnd => Theme.SelectedFill;
        public override Color ImageMarginGradientBegin => Theme.Surface;
        public override Color ImageMarginGradientMiddle => Theme.Surface;
        public override Color ImageMarginGradientEnd => Theme.Surface;
        public override Color SeparatorDark => Theme.Border;
        public override Color SeparatorLight => Theme.Border;
    }
}
