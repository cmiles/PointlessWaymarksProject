using System.Windows.Input;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.UserSettingsEditor;

public partial class UserSettingsEditorControl
{
    public UserSettingsEditorControl()
    {
        InitializeComponent();
    }

    private void OpenHyperlink(object? sender, ExecutedRoutedEventArgs e)
    {
        var toOpen = e.Parameter.ToString();
        if (string.IsNullOrEmpty(toOpen)) return;
        ProcessHelpers.OpenUrlInExternalBrowser(toOpen);
    }
}