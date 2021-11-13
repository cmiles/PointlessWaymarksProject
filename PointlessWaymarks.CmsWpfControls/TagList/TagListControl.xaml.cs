using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.TagList
{
    /// <summary>
    ///     Interaction logic for TagListControl.xaml
    /// </summary>
    public partial class TagListControl
    {
        public TagListControl()
        {
            InitializeComponent();
        }

        private void Details_Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (TagListContext) DataContext;
            viewmodel.DetailsSelectedItems = DetailsListBox?.SelectedItems.Cast<TagItemContentInformation>().ToList();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (TagListContext) DataContext;
            viewmodel.SelectedItems = TagListBox?.SelectedItems.Cast<TagListListItem>().ToList();
        }
    }
}