namespace PointlessWaymarks.PowershellRunnerData.Models;

public class RunResult
{
    public string Command { get; set; } = string.Empty;
    public DateTime CompletedOnUtc { get; set; }
    public int Id { get; set; }
    public string Output { get; set; } = string.Empty;
    public DateTime StartedOnUtc { get; set; }
}