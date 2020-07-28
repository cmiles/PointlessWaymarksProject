using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.LinkList
{
    public partial class LinkListControl : UserControl
    {
        public LinkListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (LinkListContext) DataContext;
            viewmodel.SelectedItems = ItemsListBox?.SelectedItems.Cast<LinkListListItem>().ToList();
        }
    }
}