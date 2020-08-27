using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.Xaml.Controls.Maps;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.WpfHtml;

namespace PointlessWaymarksCmsWpfControls.PointContentEditor
{
    /// <summary>
    /// Interaction logic for PointContentEditorControl.xaml
    /// </summary>
    public partial class PointContentEditorControl : UserControl
    {
        public PointContentEditorControl()
        {
            InitializeComponent();

            PointContentWebView.ScriptNotify  += PointContentWebViewOnScriptNotify;
            PointContentWebView.NavigateToString(WpfHtmlDocument.ToHtmlLeafletDocument("Point Map", string.Empty));
        }

        private void PointContentWebViewOnScriptNotify(object sender, WebViewControlScriptNotifyEventArgs e)
        {
            Debug.Write(e.Value);
        }
    }
}
