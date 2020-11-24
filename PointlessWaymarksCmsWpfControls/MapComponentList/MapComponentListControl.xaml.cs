using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.MapComponentList
{
    /// <summary>
    ///     Interaction logic for MapComponentListControl.xaml
    /// </summary>
    public partial class MapComponentListControl : UserControl
    {
        public MapComponentListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (MapComponentListContext) DataContext;
            viewmodel.SelectedItems = MapsListBox?.SelectedItems.Cast<MapComponentListListItem>().ToList();
        }
    }
}