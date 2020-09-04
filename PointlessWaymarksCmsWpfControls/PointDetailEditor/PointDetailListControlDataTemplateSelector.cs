using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.PointDetailEditor
{
    public class PointDetailListControlDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item is string identifier)
                if (identifier == "Peak")
                    return element.FindResource("NoteOnlyDetailTemplate") as DataTemplate;

            return null;
        }
    }
}