using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PointlessWaymarksCmsWpfControls.LinkStreamList
{
    public partial class LinkStreamListControl : UserControl
    {
        public LinkStreamListControl()
        {
            InitializeComponent();
        }

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

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (LinkStreamListContext) DataContext;
            viewmodel.SelectedItems = ItemsListBox?.SelectedItems.Cast<LinkStreamListListItem>().ToList();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender == null) return;

            var possibleParent = FindParent<ListBoxItem>(sender as DependencyObject);

            if (possibleParent == null) return;

            var newEvent =
                new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton, e.StylusDevice)
                {
                    RoutedEvent = MouseDownEvent, Source = sender
                };

            possibleParent.RaiseEvent(newEvent);
        }
    }
}