using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace PointlessWaymarksCmsWpfControls.PhotoList
{
    public partial class PhotoListControl : UserControl
    {
        public PhotoListControl()
        {
            InitializeComponent();
        }

        private void PhotoListBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine(e.Key);
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (PhotoListContext) DataContext;
            viewmodel.SelectedItems = PhotoListBox?.SelectedItems.Cast<PhotoListListItem>().ToList();
        }
    }
}