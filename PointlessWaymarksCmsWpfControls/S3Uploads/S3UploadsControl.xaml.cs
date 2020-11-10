using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PointlessWaymarksCmsWpfControls.S3Uploads
{
    /// <summary>
    ///     Interaction logic for S3UploadsControl.xaml
    /// </summary>
    public partial class S3UploadsControl : UserControl
    {
        public S3UploadsControl()
        {
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext == null) return;
            var viewmodel = (S3UploadsContext) DataContext;
            viewmodel.SelectedItems =
                ItemsListBox?.SelectedItems.Cast<S3UploadsItem>().ToList() ?? new List<S3UploadsItem>();
        }
    }
}