using PointlessWaymarks.CloudBackupData;

namespace PointlessWaymarks.CloudBackupRunner;

public class ConsoleAndDataNotificationProgress : IProgress<string>
{
    private readonly Guid _consoleId;

    public ConsoleAndDataNotificationProgress(Guid consoleId)
    {
        _consoleId = consoleId;
    }

    public Guid PersistentId { get; set; } = Guid.Empty;

    public void Report(string value)
    {
        Console.WriteLine(value);
        DataNotifications.PublishProgressNotification(_consoleId.ToString(), value, PersistentId);
    }
}