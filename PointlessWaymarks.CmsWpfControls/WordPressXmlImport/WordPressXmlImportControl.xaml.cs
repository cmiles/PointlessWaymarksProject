using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport
{
    /// <summary>
    ///     Interaction logic for WordPressXmlImportControl.xaml
    /// </summary>
    public partial class WordPressXmlImportControl : UserControl
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