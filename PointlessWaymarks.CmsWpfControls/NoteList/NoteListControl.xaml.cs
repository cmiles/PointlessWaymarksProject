using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.NoteList
{
    public partial class NoteListControl : UserControl
    {
        public NoteListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (NoteListContext) DataContext;
            viewmodel.SelectedItems = ItemsListBox?.SelectedItems.Cast<NoteListListItem>().ToList();
        }
    }
}