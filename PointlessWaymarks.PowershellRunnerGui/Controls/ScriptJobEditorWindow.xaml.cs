using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Interaction logic for ScriptJobEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ScriptJobEditorWindow
{
    private ScriptJobEditorWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public ScriptJobEditorContext? JobContext { get; set; }
    public StatusControlContext StatusContext { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task CreateInstance(ScriptJob toLoad, string databaseFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ScriptJobEditorWindow();

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.JobContext = await ScriptJobEditorContext.CreateInstance(window.StatusContext, toLoad, databaseFile);

        window.JobContext.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.JobContext);

        await ThreadSwitcher.ResumeForegroundAsync();

        await window.PositionWindowAndShowOnUiThread();
    }
}