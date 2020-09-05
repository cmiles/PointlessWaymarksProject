using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.PointDetailEditor
{
    public class PointDetailListControlDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element)
                switch (item)
                {
                    case PeakPointDetailContext _: return element.FindResource("PeakDetailTemplate") as DataTemplate;
                    case RestRoomPointDetailContext _:
                        return element.FindResource("RestRoomDetailTemplate") as DataTemplate;
                }

            return null;
        }
    }
}