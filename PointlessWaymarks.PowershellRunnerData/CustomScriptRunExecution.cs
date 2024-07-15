using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerData;

internal class CustomScriptRunExecution
{
    private Pipeline? _pipeline;
    internal required Guid DbId;
    internal required Guid JobId;
    internal required Guid RunId;
    internal required string ScriptToRun;
    internal required string RunnerIdentifier;

    internal CustomScriptRunExecution()
    {
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    internal async Task<(bool errors, List<string> runLog)> ExecuteScript()
    {
        // create Powershell runspace
        var runSpace = RunspaceFactory.CreateRunspace();

        // open it
        runSpace.Open();

        // create a pipeline and feed it the script text
        _pipeline = runSpace.CreatePipeline();
        _pipeline.Commands.AddScript(ScriptToRun);
        _pipeline.Input.Close();


        var returnLog = new ConcurrentBag<(DateTime, string)>();
        var errorData = false;

        _pipeline.Output.DataReady += (_, _) =>
        {
            Collection<PSObject> psObjects = _pipeline.Output.NonBlockingRead();
            foreach (var psObject in psObjects)
            {
                returnLog.Add((DateTime.UtcNow, $"{DateTime.Now:G}>> {psObject.ToString()}"));
                DataNotifications.PublishPowershellProgressNotification(RunnerIdentifier, DbId, JobId, RunId,
                    psObject.ToString());
            }
        };

        _pipeline.StateChanged += (_, eventArgs) =>
        {
            returnLog.Add((DateTime.UtcNow,
                $"{DateTime.Now:G}>> State: {eventArgs.PipelineStateInfo.State} {eventArgs.PipelineStateInfo.Reason?.ToString() ?? string.Empty}"));

            DataNotifications.PublishPowershellStateNotification(RunnerIdentifier, DbId, JobId, RunId,
                eventArgs.PipelineStateInfo.State,
                eventArgs.PipelineStateInfo.Reason?.ToString() ?? string.Empty);
        };

        _pipeline.Error.DataReady += (_, _) =>
        {
            Collection<object> errorObjects = _pipeline.Error.NonBlockingRead();
            if (errorObjects.Count == 0) return;

            errorData = true;
            foreach (var errorObject in errorObjects)
            {
                var errorString = errorObject.ToString();
                returnLog.Add((DateTime.UtcNow, $"{DateTime.Now:G}>> Error: {errorString}"));
                if (!string.IsNullOrWhiteSpace(errorString))
                    DataNotifications.PublishPowershellProgressNotification(RunnerIdentifier, DbId, JobId, RunId,
                        errorString);
            }
        };

        _pipeline.InvokeAsync();

        await Task.Delay(200);

        while (_pipeline.PipelineStateInfo.State == PipelineState.Running) await Task.Delay(250);

        return (errorData || _pipeline.HadErrors, returnLog.OrderBy(x => x.Item1).Select(x => x.Item2).ToList());
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        if (_pipeline == null) return;

        var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

        if (translatedMessage.IsT6)
        {
            var openRequest = translatedMessage.AsT6;
            if (openRequest.DatabaseId != DbId) return;

            DataNotifications.PublishOpenJobsResponse("Power Shell Runner", DbId, RunId);
            return;
        }

        if (translatedMessage.IsT5)
        {
            var cancelRequest = translatedMessage.AsT5;

            if (cancelRequest.DatabaseId != DbId) return;
            if (cancelRequest.RunPersistentId != RunId) return;

            try
            {
                _pipeline.StopAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}