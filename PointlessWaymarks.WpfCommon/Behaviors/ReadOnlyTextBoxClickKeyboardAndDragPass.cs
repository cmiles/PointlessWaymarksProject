using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.Behaviors;

public class ReadOnlyTextBoxClickKeyboardAndDragPass : Behavior<TextBox>
{
    private void AssociatedObjectOnDragEvents(object? sender, DragEventArgs e)
    {
        if (sender == null) return;

        var possibleParent = XamlHelpers.FindParent<ListBoxItem>(sender as DependencyObject);

        if (possibleParent == null) return;

        e.Handled = true;
    }

    private void AssociatedObjectOnMouseDown(object? sender, MouseButtonEventArgs e)
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

    private void AssociatedObjectOnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender == null) return;

        var possibleParent = XamlHelpers.FindParent<ListBox>(sender as DependencyObject);

        if (possibleParent == null) return;

        var newEvent =
            new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key)
            {
                RoutedEvent = UIElement.KeyDownEvent, Source = sender
            };

        e.Handled = true;

        possibleParent.RaiseEvent(newEvent);
    }

    protected override void OnAttached()
    {
        AssociatedObject.PreviewMouseDown += AssociatedObjectOnMouseDown;
        AssociatedObject.PreviewKeyDown += AssociatedObjectOnPreviewKeyDown;

        //Mark the drag previews as handled
        AssociatedObject.PreviewDragLeave += AssociatedObjectOnDragEvents;
        AssociatedObject.PreviewDragEnter += AssociatedObjectOnDragEvents;
        AssociatedObject.PreviewDragOver += AssociatedObjectOnDragEvents;
    }
}