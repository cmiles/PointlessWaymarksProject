using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.PowerShellRunnerData.Models;
using Serilog;

namespace PointlessWaymarks.PowerShellRunnerData;

public class PowerShellRunner
{
    public static async Task<ScriptJobRun?> ExecuteJob(Guid jobId, string databaseFile, string runType, Func<ScriptJobRun, Task>? callbackAfterJobFirstSave = null)
    {
        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var job = await db.ScriptJobs.FirstOrDefaultAsync(x => x.PersistentId == jobId);

        if (job == null) return null;

        var run = new ScriptJobRun { ScriptJobPersistentId = job.PersistentId, PersistentId = Guid.NewGuid(), StartedOnUtc = DateTime.UtcNow, Script = job.Script, RunType = runType };
        var obfuscationKey = await ObfuscationKeyHelpers.GetObfuscationKey(databaseFile);
        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);

        db.ScriptJobRuns.Add(run);
        await db.SaveChangesAsync();

        if (callbackAfterJobFirstSave != null) await callbackAfterJobFirstSave(run);

        (bool errors, List<string> runLog)? result = null;

        try
        {
            result = await ExecuteScript(job.Script.Decrypt(obfuscationKey), dbId, job.PersistentId, run.PersistentId, job.Name);

            run.CompletedOnUtc = DateTime.UtcNow;
            run.Output = string.Join(Environment.NewLine, result.Value.runLog).Encrypt(obfuscationKey);
            run.Errors = result.Value.errors;
        }
        catch (Exception e)
        {
            run.CompletedOnUtc = DateTime.UtcNow;
            run.Output = string.Join(Environment.NewLine, result?.runLog ?? "Null result - no output available".AsList()).Encrypt(obfuscationKey);
            run.Errors = true;

            Console.WriteLine(e);
            Log.Error(e, "Error Running Script");
        }
        finally
        {
            await db.SaveChangesAsync();
        }

        return run;
    }

    public static async Task<(bool errors, List<string> runLog)> ExecuteScript(string toInvoke, Guid databaseId, Guid jobId, Guid runId,
        string identifier)
    {
        // create Powershell runspace
        var runSpace = RunspaceFactory.CreateRunspace();

        // open it
        runSpace.Open();

        // create a pipeline and feed it the script text
        var pipeline = runSpace.CreatePipeline();
        pipeline.Commands.AddScript(toInvoke);
        pipeline.Input.Close();

        var returnLog = new ConcurrentBag<(DateTime, string)>();
        var errorData = false;

        pipeline.Output.DataReady += (_, _) =>
        {
            Collection<PSObject> psObjects = pipeline.Output.NonBlockingRead();
            foreach (var psObject in psObjects)
            {
                returnLog.Add((DateTime.UtcNow, $"{DateTime.Now:G}>> {psObject.ToString()}"));
                DataNotifications.PublishPowershellProgressNotification(identifier, databaseId, jobId, runId,
                    psObject.ToString());
            }
        };

        pipeline.StateChanged += (_, eventArgs) =>
        {
            returnLog.Add((DateTime.UtcNow,
                $"{DateTime.Now:G}>> State: {eventArgs.PipelineStateInfo.State} {eventArgs.PipelineStateInfo.Reason?.ToString() ?? string.Empty}"));

            DataNotifications.PublishPowershellStateNotification(identifier, databaseId, jobId, runId,
                eventArgs.PipelineStateInfo.State,
                eventArgs.PipelineStateInfo.Reason?.ToString() ?? string.Empty);
        };

        pipeline.Error.DataReady += (_, _) =>
        {
            Collection<object> errorObjects = pipeline.Error.NonBlockingRead();
            if(errorObjects.Count == 0) return;

            errorData = true;
            foreach (var errorObject in errorObjects)
            {
                var errorString = errorObject.ToString();
                returnLog.Add((DateTime.UtcNow, $"{DateTime.Now:G}>> Error: {errorString}"));
                if (!string.IsNullOrWhiteSpace(errorString))
                    DataNotifications.PublishPowershellProgressNotification(identifier, databaseId, jobId, runId,
                        errorString);
            }
        };

        pipeline.InvokeAsync();

        await Task.Delay(200);

        while (pipeline.PipelineStateInfo.State == PipelineState.Running) await Task.Delay(250);

        return (errorData || pipeline.HadErrors, returnLog.OrderBy(x => x.Item1).Select(x => x.Item2).ToList());
    }
}