using System.Windows;
using System.Windows.Controls;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsWpfControls.PointDetailEditor
{
    public class PointDetailListControlDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item is PointDetail pointDetail)
                if (pointDetail.DataType == "Peak")
                    return element.FindResource("NoteOnlyDetailTemplate") as DataTemplate;

            return null;
        }
    }
}