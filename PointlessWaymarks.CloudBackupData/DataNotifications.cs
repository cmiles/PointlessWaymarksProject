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
            await DataNotificationTransmissionChannel.PublishAsync(BinaryData.FromString(x)).ConfigureAwait(false)
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
        DataNotificationUpdateType updateType, Guid jobPersistentId, int? batchId)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"Data|{cleanedSender.Replace("|", " ")}|{(int)contentType}|{(int)updateType}|{jobPersistentId}|{batchId}");
    }

    public static void PublishProgressNotification(string sender, int processId, string progress, Guid jobPersistentId,
        int? batchId)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();
        var cleanedProgress = string.IsNullOrWhiteSpace(progress) ? "..." : progress.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"Progress|{cleanedSender.Replace("|", " ")}|{processId}|{jobPersistentId}|{batchId}|{cleanedProgress.Replace("|", " ")}");
    }

    public static OneOf<InterProcessDataNotification, InterProcessProgressNotification, InterProcessError>
        TranslateDataNotification(string received)
    {
        if (string.IsNullOrWhiteSpace(received))
            return new InterProcessError { ErrorMessage = "No Data" };

        try
        {
            var parsedString = received.Split("|").ToList();

            if (!parsedString.Any()
                || parsedString.Count is not 6
                || !(parsedString[0].Equals("Data") || parsedString[0].Equals("Progress"))
               )
                return new InterProcessError
                {
                    ErrorMessage = $"Data appears to be in the wrong format - {received}"
                };

            if (parsedString[0].Equals("Data"))
                return new InterProcessDataNotification
                {
                    Sender = parsedString[1],
                    ContentType = (DataNotificationContentType)int.Parse(parsedString[2]),
                    UpdateType = (DataNotificationUpdateType)int.Parse(parsedString[3]),
                    JobPersistentId = Guid.Parse(parsedString[4]),
                    BatchId = int.TryParse(parsedString[5], out var parsedBatchId) ? parsedBatchId : null
                };

            if (parsedString[0].Equals("Progress"))
                return new InterProcessProgressNotification
                {
                    Sender = parsedString[1],
                    ProcessId = int.Parse(parsedString[2]),
                    JobPersistentId = Guid.Parse(parsedString[3]),
                    BatchId = int.TryParse(parsedString[4], out var parsedBatchId) ? parsedBatchId : null,
                    ProgressMessage = parsedString[5]
                };
        }
        catch (Exception e)
        {
            return new InterProcessError { ErrorMessage = e.Message };
        }

        return new InterProcessError { ErrorMessage = "No processing occurred for this message?" };
    }
}

public record InterProcessDataNotification
{
    public int? BatchId { get; init; }
    public DataNotificationContentType ContentType { get; init; }
    public Guid JobPersistentId { get; init; }
    public string? Sender { get; init; }
    public DataNotificationUpdateType UpdateType { get; init; }
}

public record InterProcessProgressNotification
{
    public int? BatchId { get; init; }
    public Guid JobPersistentId { get; init; }
    public int ProcessId { get; init; }
    public string ProgressMessage { get; init; } = string.Empty;
    public string? Sender { get; init; }
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
    CloudCopy,
    CloudDelete,
    CloudUpload,
    Unknown,
    Progress
}