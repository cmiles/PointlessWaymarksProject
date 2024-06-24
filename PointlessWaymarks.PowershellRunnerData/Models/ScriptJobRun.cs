namespace PointlessWaymarks.PowerShellRunnerData.Models;

public class ScriptJobRun
{
    public string Command { get; set; } = string.Empty;
    public DateTime CompletedOnUtc { get; set; }
    public int Id { get; set; }
    public string Output { get; set; } = string.Empty;
    public DateTime StartedOnUtc { get; set; }
}