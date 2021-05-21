using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.CmsWpfControls.BodyContentEditor
{
    public partial class BodyContentEditorControl : UserControl
    {
        public BodyContentEditorControl()
        {
            InitializeComponent();
        }

        private void BodyContentTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is BodyContentEditorContext context)
            {
                if (sender is TextBox t)
                {
                    context.UserBodyContentUserSelectionStart = t.SelectionStart;
                    context.UserBodyContentUserSelectionLength = t.SelectionLength;
                }
            }
        }
    }
}