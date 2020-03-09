using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class ReadOnlyTextBoxClickPass : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseDown += UIElement_OnMouseDown;
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender == null) return;

            var possibleParent = XamlHelpers.FindParent<ListBoxItem>(sender as DependencyObject);

            if (possibleParent == null) return;

            var newEvent =
                new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton, e.StylusDevice)
                {
                    RoutedEvent = UIElement.MouseDownEvent, Source = sender
                };

            possibleParent.RaiseEvent(newEvent);
        }
    }
}