using System.Windows.Controls;
using System.Windows.Input;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.UserSettingsEditor
{
    public partial class UserSettingsEditorControl : UserControl
    {
        public UserSettingsEditorControl()
        {
            InitializeComponent();
        }

        private void OpenHyperlink(object sender, ExecutedRoutedEventArgs e)
        {
            ProcessHelpers.OpenUrlInExternalBrowser(e.Parameter.ToString());
        }
    }
}