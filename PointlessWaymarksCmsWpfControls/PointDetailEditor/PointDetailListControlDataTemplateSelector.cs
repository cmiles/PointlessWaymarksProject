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
                    case CampgroundPointDetailContext _:
                        return element.FindResource("CampgroundDetailTemplate") as DataTemplate;
                    case ParkingPointDetailContext _:
                        return element.FindResource("ParkingDetailTemplate") as DataTemplate;
                    case PeakPointDetailContext _: return element.FindResource("PeakDetailTemplate") as DataTemplate;
                    case RestRoomPointDetailContext _:
                        return element.FindResource("RestRoomDetailTemplate") as DataTemplate;
                    case TrailJunctionPointDetailContext _:
                        return element.FindResource("TrailJunctionDetailTemplate") as DataTemplate;
                }

            return null;
        }
    }
}