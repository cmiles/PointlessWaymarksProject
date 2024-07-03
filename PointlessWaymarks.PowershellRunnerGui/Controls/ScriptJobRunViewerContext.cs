using Microsoft.EntityFrameworkCore;
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
    private string _databaseFile = string.Empty;
    private string _key = string.Empty;
    public ScriptJob? Job { get; set; }
    public ScriptJobRun? Run { get; set; }
    public ScriptJobRunGuiView? RunView { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<ScriptJobRunViewerContext> CreateInstance(StatusControlContext? statusContext,
        Guid scriptJobRunId, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile, false);
        var key = await ObfuscationKeyHelpers.GetObfuscationKey(databaseFile);
        var run = await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == scriptJobRunId);

        if (run != null)
        {
            var jobId = run.ScriptJobPersistentId;
            var job = await db.ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == jobId);

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
                ScriptJobId = run.ScriptJobPersistentId,
                TranslatedOutput = run.Output.Decrypt(key),
                TranslatedScript = run.Script.Decrypt(key)
            };

            var toReturn = new ScriptJobRunViewerContext
            {
                StatusContext = statusContext ?? new StatusControlContext(),
                Run = run,
                Job = job,
                RunView = toAdd,
                _key = key,
                _databaseFile = databaseFile
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