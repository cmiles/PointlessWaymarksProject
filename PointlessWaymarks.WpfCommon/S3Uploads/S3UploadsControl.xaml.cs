using System.Windows.Controls;

namespace PointlessWaymarks.WpfCommon.S3Uploads;

/// <summary>
///     Interaction logic for S3UploadsControl.xaml
/// </summary>
public partial class S3UploadsControl
{
    public S3UploadsControl()
    {
        InitializeComponent();
    }

    private void Selector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var viewmodel = (S3UploadsContext)DataContext;
        if (viewmodel?.ListSelection == null) return;
        viewmodel.ListSelection.SelectedItems = ItemsListBox?.SelectedItems.Cast<S3UploadsItem>().ToList() ??
                                                [];
    }
}