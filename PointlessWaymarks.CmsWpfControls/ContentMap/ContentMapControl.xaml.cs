using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.ContentMap;

[NotifyPropertyChanged]
public partial class ContentMapControl
{
    private readonly TaskQueue _webViewWorkQueue;

    public ContentMapControl()
    {
        InitializeComponent();

        _webViewWorkQueue = new TaskQueue(true);
    }

    private async void ContentMapControl_OnLoaded(object? sender, RoutedEventArgs e)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        await ContentMapWebView.EnsureCoreWebView2Async();
    }

    private void ContentMapControlControl_OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ContentMapContext mapContext)
            _webViewWorkQueue.Enqueue(async () =>
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                ContentMapWebView.NavigateToString(
                    await WpfHtmlDocument.ToHtmlLeafletPointDocument("Content Map", null, 79, -110, string.Empty));
            });
        //RaiseMapSelectionChange += pointContext.OnRaisePointLatitudeLongitudeChange;
        //pointContext.RaisePointLatitudeLongitudeChange += PointContextOnRaisePointLatitudeLongitudeChange;
    }

    private void ContentMapWebView_OnCoreWebView2InitializationCompleted(object? sender,
        CoreWebView2InitializationCompletedEventArgs e)
    {
        _webViewWorkQueue.Suspend(false);

        ContentMapWebView.CoreWebView2.WebMessageReceived += PointContentWebViewOnScriptNotify;
    }

    private void PointContentWebViewOnScriptNotify(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var value = e.TryGetWebMessageAsString().TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(value)) return;

        if (Guid.TryParse(value, out var parsedContentId))
            RaiseMapSelectionChange?.Invoke(this, new ContentMapMapSelection(parsedContentId));
    }

    private void PointContextOnCollectionChanged(object? sender, PointLatitudeLongitudeChange e)
    {
        // _webViewWorkQueue.Enqueue(async () =>
        // {
        //     await ThreadSwitcher.ResumeForegroundAsync();
        //
        //     await ContentMapWebView.ExecuteScriptAsync(
        //         $@"pointContentMarker.setLatLng([{e.Latitude},{e.Longitude}]); map.setView([{e.Latitude},{e.Longitude}], map.getZoom());");
        // });
    }

    public event EventHandler<ContentMapMapSelection>? RaiseMapSelectionChange;
}