using System.Diagnostics;

namespace PointlessWaymarks.WpfCommon.Utility;

public static class ProcessHelpers
{
    public static async Task OpenExplorerWindowForFile(string fileName)
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();

        var ps = new ProcessStartInfo("explorer.exe", $"/select, \"{fileName}\"")
        {
            UseShellExecute = true, Verb = "open"
        };

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