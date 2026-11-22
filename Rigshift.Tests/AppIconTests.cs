using System.Xml.Linq;

namespace Rigshift.Tests;

/// <summary>
/// The executable's icon (taskbar, Explorer, Alt+Tab) is the Rigshift glyph in the accent,
/// matching the Settings and About windows, while the tray keeps the white asset it tints.
/// </summary>
public class AppIconTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void The_project_ships_the_accent_icon_as_its_application_icon()
    {
        var project = XDocument.Load(Path.Combine(RepoRoot, "Rigshift", "Rigshift.csproj"));
        var icon = project.Descendants("ApplicationIcon").Single().Value;

        Assert.Equal(@"Assets\rigshift-app.ico", icon);
    }

    [Fact]
    public void Every_frame_of_the_application_icon_is_the_accent_with_the_tray_icon_shape()
    {
        StaThread.Run(() =>
        {
            var app = Frames(Path.Combine(RepoRoot, "Rigshift", "Assets", "rigshift-app.ico"));
            var tray = Frames(Path.Combine(RepoRoot, "Rigshift", "Assets", "rigshift.ico"));

            Assert.Equal(new[] { 16, 32, 48, 256 }, app.Keys.Order());
            Assert.Equal(tray.Keys.Order(), app.Keys.Order());

            foreach (var (size, appBitmap) in app)
            {
                var trayBitmap = tray[size];
                Assert.Equal(size, appBitmap.Width);
                var opaque = 0;
                for (var x = 0; x < appBitmap.Width; x++)
                {
                    for (var y = 0; y < appBitmap.Height; y++)
                    {
                        var pixel = appBitmap.GetPixel(x, y);
                        Assert.Equal(trayBitmap.GetPixel(x, y).A, pixel.A);
                        if (pixel.A != 255)
                            continue;   // anti-aliased edge; the alpha check above covers its shape
                        opaque++;
                        Assert.Equal(0x35, pixel.R);
                        Assert.Equal(0xC9, pixel.G);
                        Assert.Equal(0xE8, pixel.B);
                    }
                }

                Assert.True(opaque > 0, $"The {size}px frame is empty.");
            }

            foreach (var bitmap in app.Values.Concat(tray.Values))
                bitmap.Dispose();
        });
    }

    // Decodes every frame of an .ico by its directory entry, so each size is read exactly
    // rather than whichever frame Icon picks as the closest match.
    private static Dictionary<int, Bitmap> Frames(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var count = BitConverter.ToUInt16(bytes, 4);
        var frames = new Dictionary<int, Bitmap>();
        for (var i = 0; i < count; i++)
        {
            var entry = 6 + 16 * i;
            var size = bytes[entry] == 0 ? 256 : bytes[entry];
            var length = BitConverter.ToInt32(bytes, entry + 8);
            var offset = BitConverter.ToInt32(bytes, entry + 12);
            var data = bytes.AsSpan(offset, length).ToArray();

            if (data.Length > 4 && data[0] == 0x89 && data[1] == (byte)'P')
            {
                using var png = new MemoryStream(data);
                using var image = new Bitmap(png);
                frames[size] = new Bitmap(image);
                continue;
            }

            // A one-frame icon around the DIB, so Icon decodes it with its alpha.
            using var single = new MemoryStream();
            using (var writer = new BinaryWriter(single, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write(bytes, entry, 8);
                writer.Write(length);
                writer.Write(22);
                writer.Write(data);
            }
            single.Position = 0;
            using var icon = new Icon(single);
            frames[size] = icon.ToBitmap();
        }
        return frames;
    }

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Rigshift.slnx")))
                return dir.FullName;
        }
        throw new DirectoryNotFoundException("Could not find the repository root above the test output.");
    }
}
