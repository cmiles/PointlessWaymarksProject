using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.ImageList
{
    public partial class ImageListControl : UserControl
    {
        public ImageListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (ImageListContext) DataContext;
            viewmodel.SelectedItems = ContentListBox?.SelectedItems.Cast<ImageListListItem>().ToList();
        }
    }
}