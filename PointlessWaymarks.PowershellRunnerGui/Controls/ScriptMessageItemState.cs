using System.Management.Automation.Runspaces;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptMessageItemState : IScriptMessageItem
{
    public Guid ScriptJobPersistentId { get; set; }
    public Guid ScriptJobRunPersistentId { get; set; }
    public string? Sender { get; set; }
    public PipelineState State { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}