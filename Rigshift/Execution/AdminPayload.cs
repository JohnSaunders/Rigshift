namespace Rigshift.Execution;

/// <summary>
/// Pure format and parse of the elevated helper's command-line payload.
/// The tokens carry no paths, process names, window titles or command text, so
/// nothing from the user-writable config can reach the elevated process.
/// The arguments are not authenticated, and safety comes from the closed
/// vocabulary. Every token is ASCII letters, digits, '-' and ':', so joining
/// them with spaces needs no quoting.
/// </summary>
public static class AdminPayload
{
    public const string Flag = "--admin";
    public const int MaxSteps = 16;
    public const int RejectedExitCode = 64;

    public static string[] Format(IReadOnlyList<Step> adminSteps)
    {
        if (adminSteps is null)
            throw new ArgumentNullException(nameof(adminSteps));
        if (adminSteps.Count == 0)
            throw new ArgumentException("Admin steps must not be empty.", nameof(adminSteps));
        if (adminSteps.Count > MaxSteps)
            throw new ArgumentException($"Admin steps must not exceed {MaxSteps}.", nameof(adminSteps));

        foreach (var step in adminSteps)
        {
            switch (step)
            {
                case PortProxyAdd add:
                    CheckPort(add.Port);
                    break;
                case PortProxyRemove remove:
                    CheckPort(remove.Port);
                    break;
                case KillWslService:
                case FlushDns:
                case KillHerdr:
                    break;
                default:
                    throw new ArgumentException($"Not an admin step: {step.GetType().Name}.", nameof(adminSteps));
            }
        }

        var tokens = new string[adminSteps.Count + 1];
        tokens[0] = Flag;
        for (int i = 0; i < adminSteps.Count; i++)
        {
            tokens[i + 1] = adminSteps[i] switch
            {
                PortProxyAdd p => $"proxy-add:{p.Port}",
                PortProxyRemove p => $"proxy-remove:{p.Port}",
                KillWslService => "kill-wslservice",
                FlushDns => "flush-dns",
                KillHerdr => "kill-herdr",
                _ => throw new ArgumentException("Not an admin step.", nameof(adminSteps))
            };
        }
        return tokens;
    }

    public static bool TryParse(string[]? args, out IReadOnlyList<Step> steps)
    {
        steps = Array.Empty<Step>();

        if (args is null || args.Length < 2 || args[0] != Flag || args.Length - 1 > MaxSteps)
            return false;

        var parsed = new Step[args.Length - 1];
        for (int i = 0; i < parsed.Length; i++)
        {
            if (!TryParseToken(args[i + 1], out var step))
                return false;
            parsed[i] = step;
        }

        steps = parsed;
        return true;
    }

    private static void CheckPort(int port)
    {
        if (port < 1 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535.");
    }

    private static bool TryParseToken(string? token, out Step step)
    {
        step = default!;
        if (token is null)
            return false;

        if (token == "kill-wslservice")
        {
            step = new KillWslService();
            return true;
        }
        if (token == "flush-dns")
        {
            step = new FlushDns();
            return true;
        }
        if (token == "kill-herdr")
        {
            step = new KillHerdr();
            return true;
        }

        int colon = token.IndexOf(':');
        if (colon < 0)
            return false;

        string verb = token[..colon];
        if (verb is not ("proxy-add" or "proxy-remove"))
            return false;
        if (!TryParsePort(token[(colon + 1)..], out int port))
            return false;

        step = verb == "proxy-remove" ? new PortProxyRemove(port) : new PortProxyAdd(port);
        return true;
    }

    // Plain decimal 1-65535: 1-5 ASCII digits, no sign, no leading zero.
    // Deliberately not int.TryParse, which accepts signs, spaces and other digit forms.
    private static bool TryParsePort(string text, out int port)
    {
        port = 0;
        if (text.Length < 1 || text.Length > 5)
            return false;

        int value = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c < '0' || c > '9')
                return false;
            if (i == 0 && c == '0')
                return false;
            value = value * 10 + (c - '0');
        }

        if (value < 1 || value > 65535)
            return false;

        port = value;
        return true;
    }
}
