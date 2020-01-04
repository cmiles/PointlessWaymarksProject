using System.Linq;
using System.Windows.Controls;

namespace TheLemmonWorkshopWpfControls.PhotoList
{
    public partial class PhotoListControl : UserControl
    {
        public PhotoListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (PhotoListContext) DataContext;
            viewmodel.SelectedItems = PhotoListBox?.SelectedItems
                .Cast<PhotoListListItem>()
                .ToList();
        }
    }
}