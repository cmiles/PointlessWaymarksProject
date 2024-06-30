namespace PointlessWaymarks.PowerShellRunnerData.Models;

public class ScriptJobRun
{
    public string Script { get; set; } = string.Empty;
    public DateTime? CompletedOnUtc { get; set; }
    public int Id { get; set; }
    public string Output { get; set; } = string.Empty;
    public int ScriptJobId { get; set; }
    public DateTime StartedOnUtc { get; set; }
    public bool Errors { get; set; }
}