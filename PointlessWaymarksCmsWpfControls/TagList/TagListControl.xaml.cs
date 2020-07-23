using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PointlessWaymarksCmsWpfControls.TagList
{
    /// <summary>
    /// Interaction logic for TagListControl.xaml
    /// </summary>
    public partial class TagListControl
    {
        public TagListControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (TagListContext)DataContext;
            viewmodel.SelectedItems = TagListBox?.SelectedItems.Cast<TagListListItem>().ToList();
        }

        private void Details_Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (TagListContext)DataContext;
            viewmodel.DetailsSelectedItems = DetailsListBox?.SelectedItems.Cast<TagItemContentInformation>().ToList();
        }
    }
}
