using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

/// <summary>
///     Interaction logic for GpxImportControl.xaml
/// </summary>
public partial class GpxImportControl
{
    public GpxImportControl()
    {
        InitializeComponent();
    }

    private void GpxImportControl_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GpxImportContext gpxContext)
        {
            gpxContext.MapRequest -= OnGpxContextOnMapRequest;
            gpxContext.MapRequest += OnGpxContextOnMapRequest;
        }
    }

    private void OnGpxContextOnMapRequest(object? _, string json)
    {
        GpxImportWebView.CoreWebView2.PostWebMessageAsJson(json);
    }

    private void GpxImportControl_OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GpxImportContext gpxContext)
        {
            gpxContext.MapRequest -= OnGpxContextOnMapRequest;
            gpxContext.MapRequest += OnGpxContextOnMapRequest;
        }
    }

    private void Selector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext == null) return;
        var viewmodel = (GpxImportContext)DataContext;
        viewmodel.SelectedItems = GpxImportListBox?.SelectedItems.Cast<IGpxImportListItem>().ToList();
    }
}