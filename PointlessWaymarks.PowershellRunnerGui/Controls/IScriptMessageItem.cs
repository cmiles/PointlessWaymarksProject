namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

public interface IScriptMessageItem
{
    string Message { get; set; }
    DateTime ReceivedOn { get; set; }
}