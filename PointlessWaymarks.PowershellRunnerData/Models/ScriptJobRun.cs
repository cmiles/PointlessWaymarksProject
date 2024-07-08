namespace PointlessWaymarks.PowerShellRunnerData.Models;

public class ScriptJobRun
{
    public DateTime? CompletedOnUtc { get; set; }
    public bool Errors { get; set; }
    public int Id { get; set; }
    public int? LengthInSeconds { get; set; }
    public string Output { get; set; } = string.Empty;
    public Guid PersistentId { get; set; }
    public string RunType { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public Guid ScriptJobPersistentId { get; set; }
    public DateTime StartedOnUtc { get; set; }
}