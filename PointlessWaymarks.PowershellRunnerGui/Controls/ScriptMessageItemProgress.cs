using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptMessageItemProgress : IScriptMessageItem
{
    public Guid ScriptJobPersistentId { get; set; }
    public Guid ScriptJobRunPersistentId { get; set; }
    public string? Sender { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}