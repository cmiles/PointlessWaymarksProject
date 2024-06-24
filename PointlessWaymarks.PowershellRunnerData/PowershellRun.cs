using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PointlessWaymarks.PowerShellRunnerData;

public class PowerShellRun
{
    public static async Task Execute(string toInvoke, int scheduleId, int runId)
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
                DataNotifications.PublishProgressNotification(nameof(Execute), scheduleId, runId,
                    psObject.ToString());
        };

        pipeline.StateChanged += (sender, eventArgs) =>
        {
            DataNotifications.PublishProgressNotification(nameof(Execute), scheduleId, runId,
                $"{eventArgs.PipelineStateInfo.State.ToString()} - {eventArgs.PipelineStateInfo.Reason}");
        };

        pipeline.Error.DataReady += (sender, eventArgs) =>
        {
            Collection<object> errorObjects = pipeline.Error.NonBlockingRead();
            foreach (var errorObject in errorObjects)
            {
                var errorString = errorObject.ToString();
                if (!string.IsNullOrWhiteSpace(errorString))
                    DataNotifications.PublishProgressNotification(nameof(Execute), scheduleId, runId,
                        errorString);
            }
        };

        pipeline.InvokeAsync();

        await Task.Delay(200);

        while (pipeline.PipelineStateInfo.State == PipelineState.Running) await Task.Delay(200);
    }
}