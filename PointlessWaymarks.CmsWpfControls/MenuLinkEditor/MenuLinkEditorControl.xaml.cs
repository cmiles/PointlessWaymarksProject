using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.MenuLinkEditor
{
    /// <summary>
    ///     Interaction logic for MenuLinkEditorControl.xaml
    /// </summary>
    public partial class MenuLinkEditorControl
    {
        public MenuLinkEditorControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (MenuLinkEditorContext) DataContext;
            viewmodel.SelectedItems = MenuLinkListBox?.SelectedItems.Cast<MenuLinkListItem>().ToList();
        }
    }
}