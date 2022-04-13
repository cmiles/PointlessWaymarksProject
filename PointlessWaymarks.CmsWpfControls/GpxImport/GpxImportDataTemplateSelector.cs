using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

public class GpxImportDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate RouteImportTemplate { get; set; }
    public DataTemplate TrackImportTemplate { get; set; }
    public DataTemplate WaypointImportTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            GpxImportWaypoint => WaypointImportTemplate,
            GpxImportTrack => TrackImportTemplate,
            GpxImportRoute => RouteImportTemplate,
            _ => null
        };
    }
}