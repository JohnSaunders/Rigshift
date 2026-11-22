using System.Text;
using Rigshift.Config;

namespace Rigshift.Settings;

/// <summary>
/// Atomically saves one config file. When the file already exists, its
/// previous bytes are preserved at the same path plus <c>.bak</c>; a later
/// save replaces that single backup with the immediately previous file.
/// A new file is created without a <c>.bak</c>.
/// </summary>
public static class ConfigFileStore
{
    /// <summary>
    /// Serializes with <see cref="ConfigLoader.ToJson"/> and writes it to
    /// <paramref name="path"/> through a temp file, so a failure never leaves
    /// a half-written destination behind.
    /// </summary>
    public static void Save(string path, RigshiftConfig config)
    {
        var json = ConfigLoader.ToJson(config);
        var temp = path + ".tmp";

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(temp, json, new UTF8Encoding(false));

            if (File.Exists(path))
                File.Replace(temp, path, path + ".bak");
            else
                File.Move(temp, path);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }
}
