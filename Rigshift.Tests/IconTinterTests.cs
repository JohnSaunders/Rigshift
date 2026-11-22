using System.Drawing.Imaging;
using Rigshift.Ui;

namespace Rigshift.Tests;

public class IconTinterTests
{
    /// <summary>A 4x4 icon: opaque top-left pixel, half-alpha next to it, rest transparent.</summary>
    private static Icon SampleIcon()
    {
        using var bmp = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
        bmp.SetPixel(0, 0, Color.FromArgb(255, 255, 255, 255));
        bmp.SetPixel(1, 0, Color.FromArgb(128, 255, 255, 255));
        return Icon.FromHandle(bmp.GetHicon());
    }

    [Fact]
    public void OpaquePixelsTakeTheTintColour()
    {
        using var source = SampleIcon();
        using var tinted = IconTinter.Tint(source, Color.FromArgb(255, 46, 107, 255));
        using var bmp = tinted.ToBitmap();

        var p = bmp.GetPixel(0, 0);
        Assert.Equal(46, p.R);
        Assert.Equal(107, p.G);
        Assert.Equal(255, p.B);
    }

    [Fact]
    public void AlphaChannelIsPreserved()
    {
        using var source = SampleIcon();
        using var tinted = IconTinter.Tint(source, Color.Red);
        using var bmp = tinted.ToBitmap();

        Assert.Equal(255, bmp.GetPixel(0, 0).A);
        Assert.InRange(bmp.GetPixel(1, 0).A, 120, 136);   // antialiased edge survives
        Assert.Equal(0, bmp.GetPixel(3, 3).A);
    }

    [Fact]
    public void SizeIsUnchanged()
    {
        using var source = SampleIcon();
        using var tinted = IconTinter.Tint(source, Color.Lime);

        Assert.Equal(source.Width, tinted.Width);
        Assert.Equal(source.Height, tinted.Height);
    }
}
