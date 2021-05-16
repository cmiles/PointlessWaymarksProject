using System.Linq;
using System.Windows.Controls;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public partial class ContentListControl : UserControl
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