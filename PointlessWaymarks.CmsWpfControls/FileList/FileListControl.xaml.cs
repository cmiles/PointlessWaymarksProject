using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.FileList
{
    public partial class FileListControl : UserControl
    {
        public FileListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (FileListContext) DataContext;
            viewmodel.SelectedItems = FileListBox?.SelectedItems.Cast<FileListListItem>().ToList();
        }
    }
}