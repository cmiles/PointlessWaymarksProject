using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.MapComponentEditor
{
    public class MapElementDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GeoJsonTemplate { get; set; }
        public DataTemplate LineTemplate { get; set; }
        public DataTemplate PointTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                MapElementListGeoJsonItem _ => GeoJsonTemplate,
                MapElementListLineItem _ => LineTemplate,
                MapElementListPointItem _ => PointTemplate,
                _ => null
            };
        }
    }
}