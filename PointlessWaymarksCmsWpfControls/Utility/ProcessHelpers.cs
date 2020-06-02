using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class ProcessHelpers
    {
        public static void OpenUrlInExternalBrowser(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (url == "about:blank") return;

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}