using System.Windows.Input;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.MarkdownDisplay;

/// <summary>
///     Interaction logic for HelpDisplayControl.xaml
/// </summary>
public partial class HelpDisplayControl
{
    public HelpDisplayControl()
    {
        InitializeComponent();
    }

    private void OpenHyperlink(object sender, ExecutedRoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Parameter.ToString())) return;
        ProcessHelpers.OpenUrlInExternalBrowser(e.Parameter.ToString());
    }
}