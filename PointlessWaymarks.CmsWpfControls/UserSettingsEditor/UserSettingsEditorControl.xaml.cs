using System.Windows.Input;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.UserSettingsEditor
{
    public partial class UserSettingsEditorControl
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