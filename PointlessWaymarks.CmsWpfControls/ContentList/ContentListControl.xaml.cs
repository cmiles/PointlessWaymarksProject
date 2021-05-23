using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public partial class ContentListControl
    {
        public ContentListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (ContentListContext) DataContext;
            viewmodel.ListSelection.SelectedItems = ContentListBox?.SelectedItems.Cast<IContentListItem>().ToList();
        }
    }
}