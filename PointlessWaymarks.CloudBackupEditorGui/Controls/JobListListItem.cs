using System.ComponentModel;
using System.Timers;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupEditorGui.Controls;

[NotifyPropertyChanged]
public partial class JobListListItem
{
    private readonly System.Timers.Timer _progressTimer = new(240000);

    public JobListListItem(BackupJob job)
    {
        DbJob = job;
        PersistentId = job.PersistentId;

        PropertyChanged += OnPropertyChanged;
        _progressTimer.Elapsed += RemoveProgress;
    }

    private void RemoveProgress(object? sender, ElapsedEventArgs e)
    {
            ProgressString = string.Empty;
            _progressTimer.Stop();
    }


    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(ProgressString) && !string.IsNullOrWhiteSpace(ProgressString))
        {

                _progressTimer.Stop();
                _progressTimer.Start();
        }
    }

    public BackupJob? DbJob { get; set; }
    public Guid PersistentId { get; set; }
    public string ProgressString { get; set; } = string.Empty;
}