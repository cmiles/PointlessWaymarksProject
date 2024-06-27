using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PointlessWaymarks.PowerShellRunnerData;

public class PowerShellRun
{
    public static async Task Execute(string toInvoke, int scheduleId, int runId, string identifier)
    {
        // create Powershell runspace
        var runSpace = RunspaceFactory.CreateRunspace();

        // open it
        runSpace.Open();

        // create a pipeline and feed it the script text
        var pipeline = runSpace.CreatePipeline();
        pipeline.Commands.AddScript(toInvoke);
        pipeline.Input.Close();

        pipeline.Output.DataReady += (sender, eventArgs) =>
        {
            Collection<PSObject> psObjects = pipeline.Output.NonBlockingRead();
            foreach (var psObject in psObjects)
                DataNotifications.PublishPowershellProgressNotification(identifier, scheduleId, runId,
                    psObject.ToString());
        };

        pipeline.StateChanged += (sender, eventArgs) =>
        {
            DataNotifications.PublishPowershellStateNotification(identifier, scheduleId, runId,
                eventArgs.PipelineStateInfo.State,
                eventArgs.PipelineStateInfo.Reason?.ToString() ?? string.Empty);
        };

        pipeline.Error.DataReady += (sender, eventArgs) =>
        {
            Collection<object> errorObjects = pipeline.Error.NonBlockingRead();
            foreach (var errorObject in errorObjects)
            {
                var errorString = errorObject.ToString();
                if (!string.IsNullOrWhiteSpace(errorString))
                    DataNotifications.PublishPowershellProgressNotification(identifier, scheduleId, runId,
                        errorString);
            }
        };

        pipeline.InvokeAsync();

        await Task.Delay(200);

        while (pipeline.PipelineStateInfo.State == PipelineState.Running) await Task.Delay(200);
    }
}