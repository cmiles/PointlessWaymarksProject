using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.PostList
{
    public partial class PostListControl : UserControl
    {
        public PostListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (PostListContext) DataContext;
            viewmodel.SelectedItems = PhotoListBox?.SelectedItems.Cast<PostListListItem>().ToList();
        }
    }
}