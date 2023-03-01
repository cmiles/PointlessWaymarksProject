using System.Windows;
using System.Windows.Media;

namespace PointlessWaymarks.WpfCommon.Utility;

public static class XamlHelpers
{
    public static T? FindChild<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent == null) return null;

        while (true)
        {
            if (VisualTreeHelper.GetChildrenCount(parent) < 1) return null;

            var childObject = VisualTreeHelper.GetChild(parent, 0);

            switch (childObject)
            {
                case T child:
                    return child;
                default:
                    parent = childObject;
                    break;
            }
        }
    }

    public static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        if (child == null) return null;

        while (true)
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            switch (parentObject)
            {
                //we've reached the end of the tree
                case null:
                    return null;
                case T parent:
                    return parent;
                default:
                    child = parentObject;
                    break;
            }
        }
    }
}