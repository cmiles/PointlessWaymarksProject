using System.Windows;
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
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance();
        var jobRun = await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == scriptJobRunId);

        var windowTitle = "Script Job Run Viewer";

        if (jobRun != null)
        {
            var job = await db.ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == jobRun.ScriptJobPersistentId);
            windowTitle = $"Job Script Run {jobRun.Id} - {jobRun.StartedOnUtc.ToLocalTime()} - Job: {job?.Name}";
        }

        var factoryContext = new StatusControlContext();

        var factoryJobRunContext =
            await ScriptJobRunViewerContext.CreateInstance(factoryContext, scriptJobRunId, databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ScriptJobRunViewerWindow()
        {
            StatusContext = factoryContext,
            JobRunContext = factoryJobRunContext,
            WindowTitle = windowTitle
        };

        await window.PositionWindowAndShowOnUiThread();
    }
}