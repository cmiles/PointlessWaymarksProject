using System.Windows.Controls;

namespace PointlessWaymarks.WpfCommon.S3Deletions;

/// <summary>
///     Interaction logic for S3DeletionsControl.xaml
/// </summary>
public partial class S3DeletionsControl
{
    public S3DeletionsControl()
    {
        InitializeComponent();
    }

    private void Selector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext == null) return;
        var viewmodel = (S3DeletionsContext)DataContext;
        viewmodel.SelectedItems = ItemsListBox?.SelectedItems.Cast<S3DeletionsItem>().ToList() ??
                                  [];
    }
}