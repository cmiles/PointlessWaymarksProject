using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptJobRunViewerContext
{
    private static string _databaseFile = string.Empty;
    private static string _key = string.Empty;
    public ScriptJob? Job { get; set; }
    public ScriptJobRun? Run { get; set; }
    public ScriptJobRunGuiView? RunView { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<ScriptJobRunViewerContext> CreateInstance(StatusControlContext? statusContext,
        int scriptJobRunId, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        _databaseFile = databaseFile;

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile, false);
        _key = await ObfuscationKeyHelpers.GetObfuscationKey(_databaseFile);
        var run = await db.ScriptJobRuns.FindAsync(scriptJobRunId);

        if (run != null)
        {
            var jobId = run.ScriptJobId;
            var job = await db.ScriptJobs.FindAsync(jobId);

            var toAdd = new ScriptJobRunGuiView
            {
                Id = run.Id,
                CompletedOnUtc = run.CompletedOnUtc,
                CompletedOn = run.CompletedOnUtc?.ToLocalTime(),
                Errors = run.Errors,
                Output = run.Output,
                RunType = run.RunType,
                Script = run.Script,
                StartedOnUtc = run.StartedOnUtc,
                StartedOn = run.StartedOnUtc.ToLocalTime(),
                ScriptJobId = run.ScriptJobId,
                TranslatedOutput = run.Output.Decrypt(_key),
                TranslatedScript = run.Script.Decrypt(_key)
            };

            var toReturn = new ScriptJobRunViewerContext
            {
                StatusContext = statusContext ?? new StatusControlContext(),
                Run = run,
                Job = job,
                RunView = toAdd
            };

            return toReturn;
        }
        else
        {
            var toReturn = new ScriptJobRunViewerContext
            {
                StatusContext = statusContext ?? new StatusControlContext(),
                Run = null,
                Job = null,
                RunView = null
            };

            toReturn.StatusContext.ShowMessageWithOkButton($"Script Job Run Id {scriptJobRunId} Not Found!",
                $"A Script Job Run with Id {scriptJobRunId} was not found in {databaseFile}??");

            return toReturn;
        }
    }
}