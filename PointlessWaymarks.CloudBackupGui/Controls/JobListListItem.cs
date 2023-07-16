using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class JobListListItem
{
    public JobListListItem(BackupJob job)
    {
        DbJob = job;
        PersistentId = job.PersistentId;
    }

    public BackupJob? DbJob { get; set; }
    public Guid PersistentId { get; set; }
    public string ProgressString { get; set; } = string.Empty;
}