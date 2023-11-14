using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.PointContentEditor;

/// <summary>
///     Interaction logic for PointContentEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class PointContentEditorWindow
{
    /// <summary>
    ///     DO NOT USE - Use CreateInstance instead - using the constructor directly will result in
    ///     core functionality being uninitialized.
    /// </summary>
    private PointContentEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public PointContentEditorContext? PointContent { get; set; }
    public StatusControlContext StatusContext { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<PointContentEditorWindow> CreateInstance(PointContent? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new PointContentEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.PointContent = await PointContentEditorContext.CreateInstance(window.StatusContext, toLoad);

        window.PointContent.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.PointContent);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}