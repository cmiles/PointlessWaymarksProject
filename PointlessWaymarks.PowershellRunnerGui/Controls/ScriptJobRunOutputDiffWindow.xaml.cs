using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

/// <summary>
///     Shows a script and output diff view with a selection list of Script Job Runs for the left and right side of the
///     diff.
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class ScriptJobRunOutputDiffWindow
{
    public ScriptJobRunOutputDiffWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    public ScriptJobRunOutputDiffContext? DiffContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = string.Empty;

    public static async Task CreateInstance(Guid initialLeftScriptJobRun, Guid? initialRightScript, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var windowTitle = "Script Job Run Output Diff";

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var leftRun = await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == initialLeftScriptJobRun);
        if (leftRun is not null)
        {
            var leftJob = await db.ScriptJobs.SingleAsync(x => x.PersistentId == leftRun.ScriptJobPersistentId);
            windowTitle = $"{leftJob.Name} - Output Run Diff";
        }

        var factoryContext = new StatusControlContext();

        var factoryDiffContext =
            await ScriptJobRunOutputDiffContext.CreateInstance(factoryContext, initialLeftScriptJobRun, initialRightScript, databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ScriptJobRunOutputDiffWindow()
        {
            StatusContext = factoryContext,
            DiffContext = factoryDiffContext,
            WindowTitle = windowTitle
        };

        await window.PositionWindowAndShowOnUiThread();
    }
}