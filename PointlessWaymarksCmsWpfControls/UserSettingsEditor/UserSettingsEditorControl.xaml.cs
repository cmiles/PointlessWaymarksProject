using System.Diagnostics;
using System.Windows.Controls;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.UserSettingsEditor
{
    public partial class UserSettingsEditorControl : UserControl
    {
        public UserSettingsEditorControl()
        {
            InitializeComponent();
        }

        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            ProcessHelpers.OpenUrlInExternalBrowser(e.Parameter.ToString());
        }
    }
}