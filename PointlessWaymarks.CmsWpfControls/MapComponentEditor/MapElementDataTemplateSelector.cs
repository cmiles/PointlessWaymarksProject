using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

public class MapElementDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FileTemplate { get; set; }
    public DataTemplate? GeoJsonTemplate { get; set; }
    public DataTemplate? ImageTemplate { get; set; }
    public DataTemplate? LineTemplate { get; set; }
    public DataTemplate? PhotoTemplate { get; set; }
    public DataTemplate? PointTemplate { get; set; }
    public DataTemplate? PostTemplate { get; set; }
    public DataTemplate? VideoTemplate { get; set; }
    
    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        return item switch
        {
            MapElementListFileItem _ => FileTemplate,
            MapElementListGeoJsonItem _ => GeoJsonTemplate,
            MapElementListImageItem _ => ImageTemplate,
            MapElementListLineItem _ => LineTemplate,
            MapElementListPointItem _ => PointTemplate,
            MapElementListPhotoItem _ => PhotoTemplate,
            MapElementListPostItem _ => PostTemplate,
            MapElementListVideoItem _ => VideoTemplate,
            _ => null
        };
    }
}