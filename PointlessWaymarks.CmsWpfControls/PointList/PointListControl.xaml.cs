using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.PointList
{
    /// <summary>
    ///     Interaction logic for PointListControl.xaml
    /// </summary>
    public partial class PointListControl : UserControl
    {
        public PointListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (PointListContext) DataContext;
            viewmodel.SelectedItems = PointsListBox?.SelectedItems.Cast<PointListListItem>().ToList();
        }
    }
}