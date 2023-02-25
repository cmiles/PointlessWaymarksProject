using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.Behaviors;

public class ListBoxOneLineMouseWheelScroll : Behavior<ListBox>
{
    private ScrollViewer? _scrollViewer;

    private void AssociatedObjectOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _scrollViewer ??= XamlHelpers.FindChild<ScrollViewer>(AssociatedObject);

        if (_scrollViewer == null) return;

        e.Handled = true;

        if (e.Delta > 0) _scrollViewer.LineUp();
        if (e.Delta < 0) _scrollViewer.LineDown();
    }

    protected override void OnAttached()
    {
        AssociatedObject.PreviewMouseWheel += AssociatedObjectOnPreviewMouseWheel;
        _scrollViewer = XamlHelpers.FindChild<ScrollViewer>(AssociatedObject);
    }
}