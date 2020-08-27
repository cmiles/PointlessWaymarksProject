using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.WpfHtml;

namespace PointlessWaymarksCmsWpfControls.PointContentEditor
{
    /// <summary>
    ///     Interaction logic for PointContentEditorControl.xaml
    /// </summary>
    public partial class PointContentEditorControl : UserControl
    {
        public PointContentEditorControl()
        {
            InitializeComponent();

            PointContentWebView.ScriptNotify += PointContentWebViewOnScriptNotify;
            PointContentWebView.NavigateToString(WpfHtmlDocument.ToHtmlLeafletDocument("Point Map", string.Empty));
        }

        private void PointContentEditorControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PointContentEditorContext pointContext)
                RaisePointLatitudeLongitudeChange += pointContext.OnRaisePointLatitudeLongitudeChange;
        }

        private void PointContentWebViewOnScriptNotify(object sender, WebViewControlScriptNotifyEventArgs e)
        {
            var value = e.Value.TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(value)) return;

            var splitValue = value.Split(";");

            if (splitValue.Length != 2) return;

            if (!double.TryParse(splitValue[0], out var latitude)) return;
            if (!double.TryParse(splitValue[1], out var longitude)) return;

            RaisePointLatitudeLongitudeChange?.Invoke(this, new PointLatitudeLongitudeChange(latitude, longitude));
        }

        public event EventHandler<PointLatitudeLongitudeChange> RaisePointLatitudeLongitudeChange;
    }
}