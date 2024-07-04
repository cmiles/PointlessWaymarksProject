using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData.Models;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptJobRunGuiView
{
    public DateTime? CompletedOn { get; set; }
    public DateTime? CompletedOnUtc { get; set; }
    public bool Errors { get; set; }
    public int Id { get; set; }
    public string Output { get; set; } = string.Empty;
    public required Guid PersistentId { get; set; }
    public string RunType { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public required Guid ScriptJobPersistentId { get; set; }
    public required DateTime StartedOn { get; set; }
    public required DateTime StartedOnUtc { get; set; }
    public string TranslatedOutput { get; set; } = string.Empty;
    public string TranslatedScript { get; set; } = string.Empty;
    public required ScriptJob Job { get; set; }
}