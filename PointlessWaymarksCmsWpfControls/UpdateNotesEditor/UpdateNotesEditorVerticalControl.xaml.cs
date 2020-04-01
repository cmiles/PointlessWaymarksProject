using System.Diagnostics;
using System.Windows.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;

namespace PointlessWaymarksCmsWpfControls.UpdateNotesEditor
{
    public partial class UpdateNotesEditorVerticalControl : UserControl
    {
        public UpdateNotesEditorVerticalControl()
        {
            InitializeComponent();
        }

        private void WebView_OnNavigationStarting(object sender, WebViewControlNavigationStartingEventArgs e)
        {
            if (e.Uri != null && e.Uri.AbsoluteUri == "about:blank")
            {
                e.Cancel = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(e.Uri?.OriginalString)) return;

            e.Cancel = true;
            var ps = new ProcessStartInfo(e.Uri?.OriginalString) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
        }
    }
}