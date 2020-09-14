using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
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
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await PointContentWebView.EnsureCoreWebView2Async(null);
            PointContentWebView.CoreWebView2.WebMessageReceived += PointContentWebViewOnScriptNotify;
        }



        private void PointContentEditorControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PointContentEditorContext pointContext)
            {
                PointContentWebView.NavigateToString(WpfHtmlDocument.ToHtmlLeafletDocument("Point Map",
                    pointContext.LatitudeEntry.UserValue, pointContext.LongitudeEntry.UserValue, string.Empty));
                RaisePointLatitudeLongitudeChange += pointContext.OnRaisePointLatitudeLongitudeChange;
                pointContext.RaisePointLatitudeLongitudeChange += PointContextOnRaisePointLatitudeLongitudeChange;
            }
        }

        private void PointContentWebViewOnScriptNotify(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var value = e.TryGetWebMessageAsString().TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(value)) return;

            var splitValue = value.Split(";");

            if (splitValue.Length != 2) return;

            if (!double.TryParse(splitValue[0], out var latitude)) return;
            if (!double.TryParse(splitValue[1], out var longitude)) return;

            RaisePointLatitudeLongitudeChange?.Invoke(this, new PointLatitudeLongitudeChange(latitude, longitude));
        }

        private void PointContextOnRaisePointLatitudeLongitudeChange(object sender, PointLatitudeLongitudeChange e)
        {
            PointContentWebView.ExecuteScriptAsync(
                $@"pointContentMarker.setLatLng([{e.Latitude},{e.Longitude}]); map.setView([{e.Latitude},{e.Longitude}], map.getZoom());").Wait();
        }

        public event EventHandler<PointLatitudeLongitudeChange> RaisePointLatitudeLongitudeChange;
    }
}