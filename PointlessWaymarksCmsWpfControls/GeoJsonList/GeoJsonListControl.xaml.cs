using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.GeoJsonList
{
    /// <summary>
    ///     Interaction logic for GeoJsonListControl.xaml
    /// </summary>
    public partial class GeoJsonListControl : UserControl
    {
        public GeoJsonListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (GeoJsonListContext) DataContext;
            viewmodel.SelectedItems = ItemsListBox?.SelectedItems.Cast<GeoJsonListListItem>().ToList();
        }
    }
}