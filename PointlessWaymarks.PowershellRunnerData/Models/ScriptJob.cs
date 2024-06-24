namespace PointlessWaymarks.PowerShellRunnerData.Models;

public class ScriptJob
{
    public string CronExpression { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Id { get; set; }
    public DateTime LastEditOn { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool ScheduleEnabled { get; set; }
    public string Script { get; set; } = string.Empty;
}