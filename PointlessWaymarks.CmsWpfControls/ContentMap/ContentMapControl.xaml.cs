using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.ContentMap;

[NotifyPropertyChanged]
public partial class ContentMapControl
{
    public ContentMapControl()
    {
        InitializeComponent();

    }
    
    private void ContentMapControl_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ContentMapContext gpxContext)
        {
            gpxContext.MapRequest -= OnContentMapContextOnMapRequest;
            gpxContext.MapRequest += OnContentMapContextOnMapRequest;
        }
    }

    private void OnContentMapContextOnMapRequest(object? _, string json)
    {
        ContentMapWebView.CoreWebView2.PostWebMessageAsJson(json);
    }

}