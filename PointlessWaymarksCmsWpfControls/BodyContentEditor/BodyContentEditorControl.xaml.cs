using System.Windows.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.BodyContentEditor
{
    public partial class BodyContentEditorControl : UserControl
    {
        public BodyContentEditorControl()
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
            ProcessHelpers.OpenUrlInExternalBrowser(e.Uri?.OriginalString);
        }
    }
}