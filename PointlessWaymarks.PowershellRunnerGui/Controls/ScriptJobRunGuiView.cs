using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public class ScriptJobRunGuiView
{
    public DateTime? CompletedOn { get; set; }
    public DateTime? CompletedOnUtc { get; set; }
    public bool Errors { get; set; }
    public int Id { get; set; }
    public string Output { get; set; } = string.Empty;
    public Guid PersistentId { get; set; }
    public string RunType { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public Guid ScriptJobId { get; set; }
    public DateTime StartedOn { get; set; }
    public DateTime StartedOnUtc { get; set; }
    public string TranslatedOutput { get; set; } = string.Empty;
    public string TranslatedScript { get; set; } = string.Empty;
}