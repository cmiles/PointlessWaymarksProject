using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JetBrains.Annotations;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class ListBoxOneLineMouseWheelScroll : Behavior<ListBox>
    {
        private ScrollViewer _scrollViewer;

        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseWheel += AssociatedObjectOnPreviewMouseWheel;
            _scrollViewer = XamlHelpers.FindChild<ScrollViewer>(AssociatedObject);
        }

        private void AssociatedObjectOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _scrollViewer ??= XamlHelpers.FindChild<ScrollViewer>(AssociatedObject);

            if (_scrollViewer == null) return;

            e.Handled = true;

            if (e.Delta > 0) _scrollViewer.LineUp();
            if(e.Delta < 0) _scrollViewer.LineDown();
        }
    }
}