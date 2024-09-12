using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.NoteContentEditor;

/// <summary>
///     Interaction logic for NoteContentEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class NoteContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private NoteContentEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
        WindowTitle = $"Note Editor - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public NoteContentEditorContext? NoteContent { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<NoteContentEditorWindow> CreateInstance(NoteContent? toLoad, bool positionAndShowWindow = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new NoteContentEditorWindow {StatusContext = await StatusControlContext.CreateInstance()};

        if (positionAndShowWindow) window.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.NoteContent = await NoteContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.NoteContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.NoteContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}