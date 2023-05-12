using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.PointDetailEditor;

public class PointDetailListControlDataTemplateSelector : DataTemplateSelector
{
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (container is FrameworkElement element)
            switch (item)
            {
                case CampgroundPointDetailContext _:
                    return element.FindResource("CampgroundDetailTemplate") as DataTemplate;
                case DrivingDirectionsPointDetailContext _:
                    return element.FindResource("DrivingDirectionsDetailTemplate") as DataTemplate;
                case FeaturePointDetailContext _:
                    return element.FindResource("FeatureDetailTemplate") as DataTemplate;
                case FeePointDetailContext _:
                    return element.FindResource("FeeDetailTemplate") as DataTemplate;
                case ParkingPointDetailContext _:
                    return element.FindResource("ParkingDetailTemplate") as DataTemplate;
                case PeakPointDetailContext _: return element.FindResource("PeakDetailTemplate") as DataTemplate;
                case RestroomPointDetailContext _:
                    return element.FindResource("RestroomDetailTemplate") as DataTemplate;
                case TrailJunctionPointDetailContext _:
                    return element.FindResource("TrailJunctionDetailTemplate") as DataTemplate;
            }

        return null;
    }
}