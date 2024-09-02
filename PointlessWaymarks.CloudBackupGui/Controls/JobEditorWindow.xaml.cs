using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CloudBackupGui.Controls;

/// <summary>
///     Interaction logic for JobEditorWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class JobEditorWindow
{
    private JobEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public WindowAccidentalClosureHelper? AccidentalCloserHelper { get; set; }
    public JobEditorContext? JobContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<JobEditorWindow> CreateInstance(BackupJob toLoad, string databaseFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new JobEditorWindow { StatusContext = await StatusControlContext.CreateInstance() };

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.JobContext = await JobEditorContext.CreateInstance(window.StatusContext, toLoad, databaseFile);

        window.JobContext.RequestContentEditorWindowClose += (_, _) => { window.Dispatcher?.Invoke(window.Close); };

        window.AccidentalCloserHelper =
            new WindowAccidentalClosureHelper(window, window.StatusContext, window.JobContext);

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}