namespace PointlessWaymarks.CloudBackupData.Models;

public class BackupSetting
{
    public DateTime CreatedOn { get; set; }
    public int Id { get; set; }
    public BackupJob Job { get; set; }
    public int JobId { get; set; }
    public DateTime LastUpdatedOn { get; set; }
    public string SettingName { get; set; }
    public string SettingValue { get; set; }
}