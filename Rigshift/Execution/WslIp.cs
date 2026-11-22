namespace Rigshift.Execution;

/// <summary>
/// Validates the first token of `wsl hostname -I` before it reaches an elevated
/// netsh command line. It must be a strict IPv4 literal: anything that controls
/// the distro's output could otherwise put arbitrary text into an argument run
/// with administrator rights.
/// </summary>
public static class WslIp
{
    public static bool TryParseFirst(string? output, out string ip)
    {
        ip = "";
        if (string.IsNullOrWhiteSpace(output))
            return false;

        var first = output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (first is null)
            return false;

        if (!System.Net.IPAddress.TryParse(first, out var address))
            return false;
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;
        if (address.ToString() != first)
            return false;

        ip = first;
        return true;
    }
}
