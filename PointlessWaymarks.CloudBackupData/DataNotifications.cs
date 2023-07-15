using System.Text;
using OneOf;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CloudBackupData;

public static class DataNotifications
{
    private static readonly string ChannelName =
        "PointlessWaymarksCloudBackup";

    private static readonly TinyMessageBus DataNotificationTransmissionChannel =
        new(ChannelName);

    private static readonly WorkQueue<string> SendMessageQueue = new()
    {
        Processor = async x =>
            await DataNotificationTransmissionChannel.PublishAsync(Encoding.UTF8.GetBytes(x)).ConfigureAwait(false)
    };

    public static bool SuspendNotifications { get; set; }

    public static TinyMessageBus NewDataNotificationChannel()
    {
        return new TinyMessageBus(ChannelName);
    }

    public static DataNotificationContentType NotificationContentTypeFromContent(dynamic? content)
    {
        return content switch
        {
            BackupJob => DataNotificationContentType.BackupJob,
            CloudDelete => DataNotificationContentType.CloudTransferBatch,
            ExcludedDirectory => DataNotificationContentType.ExcludedDirectory,
            ExcludedDirectoryNamePattern => DataNotificationContentType.ExcludedDirectoryNamePattern,
            ExcludedFileNamePattern => DataNotificationContentType.ExcludedFileNamePattern,
            _ => DataNotificationContentType.Unknown
        };
    }

    public static void PublishDataNotification(string sender, DataNotificationContentType contentType,
        DataNotificationUpdateType updateType, Guid jobPersistentId)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"Data|{cleanedSender.Replace("|", " ")}|{(int)contentType}|{(int)updateType}|{jobPersistentId}");
    }

    public static void PublishProgressNotification(string sender, string progress, Guid jobPersistentId)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();
        var cleanedProgress = string.IsNullOrWhiteSpace(progress) ? "..." : progress.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"Progress|{cleanedSender.Replace("|", " ")}|{jobPersistentId}|{cleanedProgress.Replace("|", " ")}");
    }

    public static OneOf<InterProcessDataNotification, InterProcessUpdateNotification, InterProcessError>
        TranslateDataNotification(IReadOnlyList<byte>? received)
    {
        if (received == null || received.Count == 0)
            return new InterProcessDataNotification { HasError = true, ErrorNote = "No Data" };

        try
        {
            var asString = Encoding.UTF8.GetString(received.ToArray());

            if (string.IsNullOrWhiteSpace(asString))
                return new InterProcessError { ErrorMessage = "Data is Blank" };

            var parsedString = asString.Split("|").ToList();

            if (!parsedString.Any()
                || parsedString.Count is < 4 or > 5
                || !(parsedString[0].Equals("Data") || parsedString[0].Equals("Progress"))
                || (parsedString[0].Equals("Data") && parsedString.Count is not 5)
                || (parsedString[0].Equals("Progress") && parsedString.Count is not 4)
               )
                return new InterProcessError
                {
                    ErrorMessage = $"Data appears to be in the wrong format - {asString}"
                };

            if (parsedString[0].Equals("Data"))
                return new InterProcessDataNotification
                {
                    Sender = parsedString[1],
                    ContentType = (DataNotificationContentType)int.Parse(parsedString[2]),
                    UpdateType = (DataNotificationUpdateType)int.Parse(parsedString[3]),
                    JobPersistentId = Guid.Parse(parsedString[4])
                };

            if (parsedString[0].Equals("Progress"))
                return new InterProcessUpdateNotification
                {
                    Sender = parsedString[1],
                    JobPersistentId = Guid.Parse(parsedString[2]),
                    UpdateMessage = parsedString[3]
                };
        }
        catch (Exception e)
        {
            return new InterProcessError { ErrorMessage = e.Message };
        }

        return new InterProcessError { ErrorMessage = "No processing occured for this message?" };
    }
}

public record InterProcessDataNotification
{
    public DataNotificationContentType ContentType { get; init; }
    public string? ErrorNote { get; init; }
    public bool HasError { get; init; }
    public Guid JobPersistentId { get; set; }
    public string? Sender { get; init; }
    public DataNotificationUpdateType UpdateType { get; init; }
}

public record InterProcessUpdateNotification
{
    public string? ErrorNote { get; init; }
    public bool HasError { get; init; }
    public Guid JobPersistentId { get; set; }
    public string? Sender { get; init; }
    public string UpdateMessage { get; init; } = string.Empty;
}

public record InterProcessError
{
    public string ErrorMessage = string.Empty;
}

public enum DataNotificationUpdateType
{
    New,
    Update,
    Delete
}

public enum DataNotificationContentType
{
    BackupJob,
    CloudTransferBatch,
    ExcludedDirectory,
    ExcludedDirectoryNamePattern,
    ExcludedFileNamePattern,
    Unknown,
    Progress
}