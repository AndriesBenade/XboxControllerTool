using Microsoft.Win32;

namespace XboxControllerTool.Windows;

public readonly record struct BrowserChoice(string DisplayName, string? ExecutablePath)
{
    /// <summary>The entry meaning "whatever Windows itself is set to open links with".</summary>
    public static BrowserChoice SystemDefault => new("System Default", null);

    public bool IsSystemDefault => ExecutablePath is null;
}

/// <summary>
/// Lists the browsers actually installed on this machine, read from the registry key Windows itself
/// uses to populate its own default-browser list. Only entries whose executable is really on disk
/// are returned, so the picker can never offer a browser that cannot be launched.
/// </summary>
public static class BrowserCatalog
{
    private const string StartMenuInternet = @"SOFTWARE\Clients\StartMenuInternet";

    public static IReadOnlyList<BrowserChoice> Discover()
    {
        var found = new Dictionary<string, BrowserChoice>(StringComparer.OrdinalIgnoreCase);

        CollectFrom(Registry.LocalMachine, found);
        CollectFrom(Registry.CurrentUser, found);

        return
        [
            BrowserChoice.SystemDefault,
            .. found.Values.OrderBy(browser => browser.DisplayName, StringComparer.OrdinalIgnoreCase)
        ];
    }

    public static BrowserChoice Resolve(IReadOnlyList<BrowserChoice> browsers, string? executablePath)
    {
        if (executablePath is null)
        {
            return BrowserChoice.SystemDefault;
        }

        foreach (var browser in browsers)
        {
            if (string.Equals(browser.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase))
            {
                return browser;
            }
        }

        // A browser that was chosen and has since been uninstalled is still named rather than
        // silently reverting, so the setting screen can show what is actually stored.
        return new BrowserChoice(Path.GetFileNameWithoutExtension(executablePath), executablePath);
    }

    private static void CollectFrom(RegistryKey root, Dictionary<string, BrowserChoice> found)
    {
        try
        {
            using var clients = root.OpenSubKey(StartMenuInternet);

            if (clients is null)
            {
                return;
            }

            foreach (var name in clients.GetSubKeyNames())
            {
                if (TryReadBrowser(clients, name) is { } browser && browser.ExecutablePath is not null)
                {
                    found.TryAdd(browser.ExecutablePath, browser);
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
        }
    }

    private static BrowserChoice? TryReadBrowser(RegistryKey clients, string name)
    {
        try
        {
            using var browserKey = clients.OpenSubKey(name);

            if (browserKey is null)
            {
                return null;
            }

            using var commandKey = browserKey.OpenSubKey(@"shell\open\command");

            if (commandKey?.GetValue(null) is not string command || string.IsNullOrWhiteSpace(command))
            {
                return null;
            }

            var executablePath = CommandLine.ExtractExecutablePath(command);

            if (executablePath is null || !File.Exists(executablePath))
            {
                return null;
            }

            var displayName = browserKey.GetValue(null) as string;

            return new BrowserChoice(
                string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(executablePath) : displayName,
                executablePath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            return null;
        }
    }
}

public static class CommandLine
{
    /// <summary>
    /// Pulls the executable out of a registry shell command. Quoting is optional in these values -
    /// Internet Explorer, for one, is registered unquoted - so an unquoted path cannot simply be cut
    /// at the first space or "C:\Program Files\..." would lose everything after "C:\Program".
    /// The executable is instead taken up to the first ".exe" that ends the path.
    /// </summary>
    public static string? ExtractExecutablePath(string command)
    {
        var trimmed = command.Trim();

        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.StartsWith('"'))
        {
            var closingQuoteIndex = trimmed.IndexOf('"', 1);
            return closingQuoteIndex > 1 ? trimmed[1..closingQuoteIndex] : null;
        }

        var searchFrom = 0;

        while (searchFrom < trimmed.Length)
        {
            var extensionIndex = trimmed.IndexOf(".exe", searchFrom, StringComparison.OrdinalIgnoreCase);

            if (extensionIndex < 0)
            {
                break;
            }

            var end = extensionIndex + 4;

            if (end == trimmed.Length || char.IsWhiteSpace(trimmed[end]))
            {
                return trimmed[..end];
            }

            searchFrom = end;
        }

        var spaceIndex = trimmed.IndexOf(' ');
        return spaceIndex > 0 ? trimmed[..spaceIndex] : trimmed;
    }
}
