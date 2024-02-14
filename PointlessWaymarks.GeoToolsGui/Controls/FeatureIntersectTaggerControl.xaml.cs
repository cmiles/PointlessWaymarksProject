using System.Windows.Input;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.GeoToolsGui.Controls;

/// <summary>
///     Interaction logic for FeatureIntersectTaggerControl.xaml
/// </summary>
public partial class FeatureIntersectTaggerControl
{
    public FeatureIntersectTaggerControl()
    {
        InitializeComponent();
    }

    private void OpenHyperlink(object sender, ExecutedRoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Parameter.ToString())) return;
        ProcessHelpers.OpenUrlInExternalBrowser(e.Parameter.ToString());
    }
}