using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsWpfControls.MapComponentEditor
{
    public class MapElementDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PointTemplate { get; set; }
        public DataTemplate LineTemplate { get; set; }
        public DataTemplate GeoJsonTemplate { get; set; }

        public override DataTemplate
            SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                GeoJsonContent _ => GeoJsonTemplate,
                LineContent _ => LineTemplate,
                PointContent _ => PointTemplate,
                _ => null
            };
        }
    }
}
