using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.LinkStreamList
{
    public partial class LinkStreamListControl : UserControl
    {
        public LinkStreamListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (LinkStreamListContext) DataContext;
            viewmodel.SelectedItems = ItemsListBox?.SelectedItems.Cast<LinkStreamListListItem>().ToList();
        }
    }
}