namespace PointlessWaymarks.PowerShellRunnerData.Models;

public class Schedule
{
    public string CronExpression { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ScheduleCommand { get; set; } = string.Empty;
    public bool ScheduleEnabled { get; set; }
}