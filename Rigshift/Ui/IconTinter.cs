using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Rigshift.Ui;

/// <summary>
/// Recolors an icon's opaque pixels while preserving alpha, so the shipped
/// white silhouette can carry any loadout color without a separate asset.
/// </summary>
[SupportedOSPlatform("windows")]
public static partial class IconTinter
{
    public static Icon Tint(Icon source, Color color)
    {
        using var bitmap = source.ToBitmap();
        using var tinted = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var src = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var dst = tinted.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            var count = bitmap.Width * bitmap.Height * 4;
            var pixels = new byte[count];
            Marshal.Copy(src.Scan0, pixels, 0, count);

            // BGRA layout; keep index 3 (alpha), overwrite the color channels.
            for (var i = 0; i < count; i += 4)
            {
                pixels[i]     = color.B;
                pixels[i + 1] = color.G;
                pixels[i + 2] = color.R;
            }

            Marshal.Copy(pixels, 0, dst.Scan0, count);
        }
        finally
        {
            bitmap.UnlockBits(src);
            tinted.UnlockBits(dst);
        }

        // Copy off the HICON immediately so the GDI handle can be released.
        var handle = tinted.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr handle);
}
