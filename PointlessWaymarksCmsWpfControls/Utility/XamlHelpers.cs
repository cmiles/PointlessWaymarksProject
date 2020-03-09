using System.Windows;
using System.Windows.Media;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class XamlHelpers
    {
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;

            while (true)
            {
                //get parent item
                var parentObject = VisualTreeHelper.GetParent(child);

                switch (parentObject)
                {
                    //we've reached the end of the tree
                    case null:
                        return null;
                    //check if the parent matches the type we're looking for
                    case T parent:
                        return parent;
                    default:
                        child = parentObject;
                        break;
                }
            }
        }
    }
}