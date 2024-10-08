using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Interaction logic for ScriptJobRunViewerWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class ScriptJobRunViewerWindow
{
    public ScriptJobRunViewerWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    public ScriptJobRunViewerContext? JobRunContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = string.Empty;

    public static async Task CreateInstance(Guid scriptJobRunId, string databaseFile)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var jobRun = await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == scriptJobRunId);

        var windowTitle = "Script Job Run Viewer";

        if (jobRun != null)
        {
            var job = await db.ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == jobRun.ScriptJobPersistentId);
            windowTitle =
                $"Job Script Run {jobRun.PersistentId} - {jobRun.StartedOnUtc.ToLocalTime()} - Job: {job?.Name}";
        }

        var factoryJobRunContext =
            await ScriptJobRunViewerContext.CreateInstance(factoryStatusContext, scriptJobRunId, databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ScriptJobRunViewerWindow
        {
            StatusContext = factoryStatusContext,
            JobRunContext = factoryJobRunContext,
            WindowTitle = windowTitle
        };

        await window.PositionWindowAndShowOnUiThread();
    }
}