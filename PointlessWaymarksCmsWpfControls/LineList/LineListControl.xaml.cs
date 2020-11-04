using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.LineList
{
    /// <summary>
    ///     Interaction logic for LineListControl.xaml
    /// </summary>
    public partial class LineListControl : UserControl
    {
        public LineListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (LineListContext) DataContext;
            viewmodel.SelectedItems = ItemsListBox?.SelectedItems.Cast<LineListListItem>().ToList();
        }
    }
}