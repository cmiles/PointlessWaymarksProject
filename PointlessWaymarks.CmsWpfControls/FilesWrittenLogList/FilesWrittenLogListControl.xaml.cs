using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.FilesWrittenLogList
{
    /// <summary>
    ///     Interaction logic for FilesWrittenLogListControl.xaml
    /// </summary>
    public partial class FilesWrittenLogListControl
    {
        public FilesWrittenLogListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (FilesWrittenLogListContext) DataContext;
            viewmodel.SelectedItems =
                WrittenFilesDataGrid?.SelectedItems.Cast<FilesWrittenLogListListItem>().ToList() ??
                new List<FilesWrittenLogListListItem>();
        }
    }
}