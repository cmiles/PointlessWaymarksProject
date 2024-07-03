using System.Management.Automation.Runspaces;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptProgressMessageItem : IPowerShellProgress
{
    public Guid ScriptJobPersistentId { get; set; }
    public Guid ScriptJobRunPersistentId { get; set; }
    public string? Sender { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}

public interface IPowerShellProgress
{
    string Message { get; set; }
    DateTime ReceivedOn { get; set; }
}

[NotifyPropertyChanged]
public partial class ScriptStateMessageItem : IPowerShellProgress
{
    public Guid ScriptJobPersistentId { get; set; }
    public Guid ScriptJobRunPersistentId { get; set; }
    public string? Sender { get; set; }
    public PipelineState State { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}

[NotifyPropertyChanged]
public partial class ScriptErrorMessageItem : IPowerShellProgress
{
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}