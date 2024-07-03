using System.Management.Automation.Runspaces;
using System.Text;
using OneOf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.PowerShellRunnerData.Models;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerData;

public static class DataNotifications
{
    public enum DataNotificationContentType
    {
        ScriptJob,
        ScriptJobRun,
        PowerShellRunnerSettings,
        Unknown,
        Progress
    }

    public enum DataNotificationUpdateType
    {
        New,
        Update,
        Delete
    }

    private static readonly string ChannelName =
        "PointlessWaymarksPowershellRunner";

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
            ScriptJob => DataNotificationContentType.ScriptJob,
            ScriptJobRun => DataNotificationContentType.ScriptJobRun,
            PowerShellRunnerSetting => DataNotificationContentType.PowerShellRunnerSettings,
            _ => DataNotificationContentType.Unknown
        };
    }

    public static void PublishDataNotification(string sender, DataNotificationContentType contentType,
        DataNotificationUpdateType updateType, Guid databaseId, Guid persistentId)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"{nameof(InterProcessDataNotification)}|{cleanedSender.Replace("|", " ")}|{(int)contentType}|{(int)updateType}|{databaseId}|{persistentId}");
    }

    public static void PublishPowershellProgressNotification(string sender, Guid databaseId, Guid scriptJobId, Guid runId,
        string progress)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();
        var cleanedProgress = string.IsNullOrWhiteSpace(progress) ? "..." : progress.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"{nameof(InterProcessPowershellProgressNotification)}|{cleanedSender.Replace("|", " ")}|{databaseId}|{scriptJobId}|{runId}|{cleanedProgress.Replace("|", " ")}");
    }

    public static void PublishPowershellStateNotification(string sender, Guid databaseId, Guid scriptJobId, Guid runId,
        PipelineState state,
        string reason)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();
        var cleanedProgress = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();

        SendMessageQueue.Enqueue(
            $"{nameof(InterProcessPowershellStateNotification)}|{cleanedSender.Replace("|", " ")}|{databaseId}|{scriptJobId}|{runId}|{state}|{cleanedProgress.Replace("|", " ")}");
    }

    public static OneOf<InterProcessDataNotification, InterProcessPowershellProgressNotification,
            InterProcessPowershellStateNotification, InterProcessProcessingError>
        TranslateDataNotification(IReadOnlyList<byte>? received)
    {
        if (received == null || received.Count == 0)
            return new InterProcessProcessingError { ErrorMessage = "No Data" };

        try
        {
            var asString = Encoding.UTF8.GetString(received.ToArray());

            if (string.IsNullOrWhiteSpace(asString))
                return new InterProcessProcessingError { ErrorMessage = "Data is Blank" };

            var parsedString = asString.Split("|").ToList();

            if (!parsedString.Any()
                || !(parsedString.Count is 6 or 7)
                || !(parsedString[0].Equals(nameof(InterProcessDataNotification)) || parsedString[0].Equals(nameof(InterProcessPowershellProgressNotification)) ||
                     parsedString[0].Equals(nameof(InterProcessPowershellStateNotification)))
               )
                return new InterProcessProcessingError
                {
                    ErrorMessage = $"Data appears to be in the wrong format - {asString}"
                };

            if (parsedString[0].Equals(nameof(InterProcessDataNotification)))
                return new InterProcessDataNotification
                {
                    Sender = parsedString[1],
                    ContentType = (DataNotificationContentType)int.Parse(parsedString[2]),
                    UpdateType = (DataNotificationUpdateType)int.Parse(parsedString[3]),
                    PersistentId = Guid.TryParse(parsedString[4], out var parsedBatchId) ? parsedBatchId : Guid.Empty
                };

            if (parsedString[0].Equals(nameof(InterProcessPowershellProgressNotification)))
                return new InterProcessPowershellProgressNotification
                {
                    Sender = parsedString[1],
                    ScriptJobPersistentId = Guid.TryParse(parsedString[2], out var parsedRunScheduleId)
                        ? parsedRunScheduleId
                        : Guid.Empty,
                    ScriptJobRunPersistentId = Guid.TryParse(parsedString[3], out var parsedRunResultId)
                        ? parsedRunResultId
                        : Guid.Empty,
                    ProgressMessage = parsedString[4]
                };

            if (parsedString[0].Equals(nameof(InterProcessPowershellStateNotification)))
                return new InterProcessPowershellStateNotification
                {
                    Sender = parsedString[1],
                    ScriptJobId = Guid.TryParse(parsedString[2], out var parsedRunScheduleId)
                        ? parsedRunScheduleId
                        : Guid.Empty,
                    ScriptJobRunId = Guid.TryParse(parsedString[3], out var parsedRunResultId)
                        ? parsedRunResultId
                        : Guid.Empty,
                    State = Enum.Parse<PipelineState>(parsedString[4]),
                    ProgressMessage = parsedString[5]
                };
        }
        catch (Exception e)
        {
            return new InterProcessProcessingError { ErrorMessage = e.Message };
        }

        return new InterProcessProcessingError { ErrorMessage = "No processing occurred for this message?" };
    }

    public record InterProcessDataNotification
    {
        public DataNotificationContentType ContentType { get; init; }
        public Guid DatabaseId { get; set; }
        public Guid PersistentId { get; init; }
        public string? Sender { get; init; }
        public DataNotificationUpdateType UpdateType { get; init; }
    }

    public record InterProcessProcessingError
    {
        public string ErrorMessage = string.Empty;
    }

    public record InterProcessPowershellProgressNotification
    {
        public Guid DatabaseId { get; set; }
        public string ProgressMessage { get; init; } = string.Empty;
        public Guid ScriptJobPersistentId { get; init; }
        public Guid ScriptJobRunPersistentId { get; init; }
        public string? Sender { get; init; }
    }

    public record InterProcessPowershellStateNotification
    {
        public Guid DatabaseId { get; set; }
        public string ProgressMessage { get; init; } = string.Empty;
        public Guid ScriptJobId { get; init; }
        public Guid ScriptJobRunId { get; init; }
        public string? Sender { get; init; }
        public PipelineState State { get; set; }
    }
}