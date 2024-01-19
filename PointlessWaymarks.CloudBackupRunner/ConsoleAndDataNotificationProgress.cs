using PointlessWaymarks.CloudBackupData;

namespace PointlessWaymarks.CloudBackupRunner;

public class ConsoleAndDataNotificationProgress(Guid consoleId) : IProgress<string>
{
    public readonly Guid _consoleId = consoleId;

    public Guid PersistentId { get; set; } = Guid.Empty;
    public int? BatchId { get; set; }

    public void Report(string value)
    {
        Console.WriteLine(value);
        DataNotifications.PublishProgressNotification(_consoleId.ToString(), Environment.ProcessId, value, PersistentId, BatchId);
    }
}