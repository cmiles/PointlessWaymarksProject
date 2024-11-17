using System.ComponentModel;
using System.Diagnostics;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SitePictureSizesEditor;

/// <summary>
///     Interaction logic for SitePictureSizesEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class SitePictureSizesEditorWindow
{
    public SitePictureSizesEditorWindow(SitePictureSizesEditorContext toLoad, StatusControlContext statusControlContext)
    {
        InitializeComponent();
        StatusContext = statusControlContext;
        SitePictureSizesEditorContext = toLoad;
        DataContext = this;
        WindowTitle = $"Picture Size Generation Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public bool CheckForChangesOnClose { get; set; } = true;
    public SitePictureSizesEditorContext SitePictureSizesEditorContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<SitePictureSizesEditorWindow> CreateInstance(SitePictureSizesEditorContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryContext = await StatusControlContext.CreateInstance(null);

        var editorContext = toLoad ?? await SitePictureSizesEditorContext.CreateInstance(factoryContext);

        var window =
            new SitePictureSizesEditorWindow(
                editorContext, factoryContext);

        editorContext.CloseWindowRequest += window.EditorContextOnCloseWindowRequest;

        return window;
    }

    private async void EditorContextOnCloseWindowRequest(object? sender, EventArgs e)
    {
        try
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            Close();
        }
        catch (Exception ex)
        {
            StatusContext.ToastError($"Error Closing Window {ex.Message}");
        }
    }

    private async void SitePictureSizesEditorWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        try
        {
            if (CheckForChangesOnClose)
            {
                var changes = SitePictureSizesEditorContext.CheckForChanges();
                if (changes.hasChanges)
                {
                    e.Cancel = true;

                    if ((await StatusContext.ShowMessageWithYesNoButton("Discard Changes?",
                            $"There are unsaved changes - do you want to leave and discard these changes?{Environment.NewLine}{Environment.NewLine}{changes.changeNotes}"))
                        .Equals("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        CheckForChangesOnClose = false;
                        Close();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ignored Exception in the {nameof(SitePictureSizesEditorWindow)} close check - {ex}");
        }
    }
}