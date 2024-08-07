using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptMessageItemError : IScriptMessageItem
{
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}