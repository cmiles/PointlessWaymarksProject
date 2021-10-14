using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointContentEditor
{
    /// <summary>
    ///     Interaction logic for PointContentEditorControl.xaml
    /// </summary>
    public partial class PointContentEditorControl
    {
        private readonly TaskQueue _webViewWorkQueue;

        public PointContentEditorControl()
        {
            InitializeComponent();

            _webViewWorkQueue = new TaskQueue(true);
        }

        private void PointContentEditorControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PointContentEditorContext pointContext)
            {
                _webViewWorkQueue.Enqueue(async () =>
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    PointContentWebView.NavigateToString(WpfHtmlDocument.ToHtmlLeafletPointDocument("Point Map",
                        pointContext.LatitudeEntry.UserValue, pointContext.LongitudeEntry.UserValue, string.Empty));
                });

                RaisePointLatitudeLongitudeChange += pointContext.OnRaisePointLatitudeLongitudeChange;
                pointContext.RaisePointLatitudeLongitudeChange += PointContextOnRaisePointLatitudeLongitudeChange;
            }
        }

        private async void PointContentEditorControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            await PointContentWebView.EnsureCoreWebView2Async();
        }

        private void PointContentWebView_OnCoreWebView2InitializationCompleted(object sender,
            CoreWebView2InitializationCompletedEventArgs e)
        {
            _webViewWorkQueue.Suspend(false);

            PointContentWebView.CoreWebView2.WebMessageReceived += PointContentWebViewOnScriptNotify;
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
            _webViewWorkQueue.Enqueue(async () =>
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                await PointContentWebView.ExecuteScriptAsync(
                    $@"pointContentMarker.setLatLng([{e.Latitude},{e.Longitude}]); map.setView([{e.Latitude},{e.Longitude}], map.getZoom());");
            });
        }

        public event EventHandler<PointLatitudeLongitudeChange> RaisePointLatitudeLongitudeChange;
    }
}