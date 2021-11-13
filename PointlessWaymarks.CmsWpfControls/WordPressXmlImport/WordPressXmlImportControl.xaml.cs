using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport
{
    /// <summary>
    ///     Interaction logic for WordPressXmlImportControl.xaml
    /// </summary>
    public partial class WordPressXmlImportControl
    {
        public WordPressXmlImportControl()
        {
            InitializeComponent();
        }


        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (WordPressXmlImportContext) DataContext;
            viewmodel.SelectedItems = ItemsListBox?.SelectedItems.Cast<WordPressXmlImportListItem>().ToList() ??
                                      new List<WordPressXmlImportListItem>();
        }
    }
}