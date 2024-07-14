namespace PointlessWaymarks.PowerShellRunnerData.Models;

public class ScriptJob
{
    public bool AllowSimultaneousRuns { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public int DeleteScriptJobRunsAfterMonths { get; set; } = 12;
    public string Description { get; set; } = string.Empty;
    public int Id { get; set; }
    public DateTime LastEditOn { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PersistentId { get; set; }
    public bool ScheduleEnabled { get; set; }
    public string Script { get; set; } = string.Empty;
}