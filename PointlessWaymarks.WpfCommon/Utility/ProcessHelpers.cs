using System.Diagnostics;

namespace PointlessWaymarks.WpfCommon.Utility;

public static class ProcessHelpers
{
    public static async Task OpenExplorerWindowForDirectory(string directoryName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo("explorer.exe", $" \"{directoryName}\"")
        {
            UseShellExecute = true, Verb = "open"
        };

        Process.Start(ps);
    }

    public static async Task OpenExplorerWindowForFile(string fileName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var args = $"/e, /select, \"{fileName.Replace("/", "\\")}\"";

        var ps = new ProcessStartInfo { FileName = "explorer", Arguments = args };
        Process.Start(ps);
    }

    public static void OpenUrlInExternalBrowser(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (url == "about:blank") return;

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}