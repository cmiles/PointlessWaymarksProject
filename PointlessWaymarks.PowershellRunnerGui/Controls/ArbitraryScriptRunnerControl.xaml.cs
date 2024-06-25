using System.Windows;
using System.Windows.Controls;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Interaction logic for ArbitraryScriptRunnerControl.xaml
/// </summary>
public partial class ArbitraryScriptRunnerControl
{
    private ScrollViewer? _scrollViewer;

    public ArbitraryScriptRunnerControl()
    {
        InitializeComponent();
    }

    //private void ArbitraryScriptRunnerControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    //{
    //    if (DataContext is ArbitraryScriptRunnerContext context)
    //        context.Items.CollectionChanged += (o, args) =>
    //        {
    //            if (_scrollViewer == null)
    //            {
    //                _scrollViewer =
    //                    XamlHelpers.GetDescendantByType(ProgressListBox, typeof(ScrollViewer)) as ScrollViewer;
    //                if (_scrollViewer != null) _scrollViewer.ScrollChanged += ScrollViewerOnScrollChanged;
    //            }
    //        };
    //}

    //private void ScrollViewerOnScrollChanged(object sender, ScrollChangedEventArgs e)
    //{
    //    if (e.ExtentHeightChange != 0)
    //    {
    //        var scrollViewer = sender as ScrollViewer;
    //        scrollViewer?.ScrollToBottom();
    //    }
    //}
}