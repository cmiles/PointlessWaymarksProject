using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

/// <summary>
///     Interaction logic for GpxImportControl.xaml
/// </summary>
public partial class GpxImportControl
{
    public GpxImportControl()
    {
        InitializeComponent();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext == null) return;
        var viewmodel = (GpxImportContext)DataContext;
        viewmodel.SelectedItems = GpxImportListBox?.SelectedItems.Cast<IGpxImportListItem>().ToList();
    }
}