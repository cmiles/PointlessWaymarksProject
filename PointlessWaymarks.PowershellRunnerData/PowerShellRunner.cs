using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PointlessWaymarks.PowerShellRunnerData.Models;

namespace PointlessWaymarks.PowerShellRunnerData;

public class PowerShellRunner
{
    public static async Task CleanUpOrphanRuns(string databaseFile, Guid databaseId)
    {
        var newCleaner = new PowerShellRunnerCloseOrphanRuns { DatabaseFile = databaseFile, DatabaseId = databaseId };
        await newCleaner.CloseOrphans();
    }

    public static async Task<ScriptJobRun?> ExecuteJob(Guid jobId, bool allowSimultaneousRuns, string databaseFile,
        string runType,
        Func<ScriptJobRun, Task>? callbackAfterJobFirstSave = null)
    {
        var runner = new JobScriptRunExecution
        {
            AllowSimultaneousRuns = allowSimultaneousRuns, CallbackAfterJobFirstSave = callbackAfterJobFirstSave,
            DatabaseFile = databaseFile, JobId = jobId, RunType = runType
        };

        return await runner.ExecuteJob();
    }

    public static async Task<(bool errors, List<string> runLog)> ExecuteScript(string toInvoke, ScriptType type, Guid databaseId,
        Guid jobId, Guid runId,
        string identifier)
    {
        var runner = new CustomScriptRunExecution
        {
            DbId = databaseId, JobId = jobId, RunId = runId, ScriptToRun = toInvoke, RunnerIdentifier = identifier, ScriptStyle = type
        };

        return await runner.ExecuteScript();
    }
}