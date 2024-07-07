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

    public static void PublishJobDataNotification(string sender, DataNotificationUpdateType updateType, Guid databaseId,
        Guid jobPersistentId)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"{nameof(InterProcessJobDataNotification)}|{cleanedSender.Replace("|", " ")}|{(int)updateType}|{databaseId}|{jobPersistentId}");
    }

    public static void PublishPowershellProgressNotification(string sender, Guid databaseId, Guid scriptJobId,
        Guid runId,
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

    public static void PublishRunDataNotification(string sender, DataNotificationUpdateType updateType, Guid databaseId,
        Guid jobPersistentId, Guid runPersistentId)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"{nameof(InterProcessRunDataNotification)}|{cleanedSender.Replace("|", " ")}|{(int)updateType}|{databaseId}|{jobPersistentId}|{runPersistentId}");
    }

    public static OneOf<InterProcessJobDataNotification, InterProcessRunDataNotification,
            InterProcessPowershellProgressNotification,
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
                || !(parsedString.Count is 5 or 6 or 7)
                || !(parsedString[0].Equals(nameof(InterProcessJobDataNotification)) ||
                     parsedString[0].Equals(nameof(InterProcessRunDataNotification)) ||
                     parsedString[0].Equals(nameof(InterProcessPowershellProgressNotification)) ||
                     parsedString[0].Equals(nameof(InterProcessPowershellStateNotification)))
               )
                return new InterProcessProcessingError
                {
                    ErrorMessage = $"Data appears to be in the wrong format - {asString}"
                };

            if (parsedString[0].Equals(nameof(InterProcessJobDataNotification)))
                return new InterProcessJobDataNotification
                {
                    Sender = parsedString[1],
                    UpdateType = (DataNotificationUpdateType)int.Parse(parsedString[2]),
                    DatabaseId = Guid.TryParse(parsedString[3], out var parsedDbId) ? parsedDbId : Guid.Empty,
                    JobPersistentId = Guid.TryParse(parsedString[4], out var parsedJobId) ? parsedJobId : Guid.Empty
                };

            if (parsedString[0].Equals(nameof(InterProcessRunDataNotification)))
                return new InterProcessRunDataNotification
                {
                    Sender = parsedString[1],
                    UpdateType = (DataNotificationUpdateType)int.Parse(parsedString[2]),
                    DatabaseId = Guid.TryParse(parsedString[3], out var parsedDbId) ? parsedDbId : Guid.Empty,
                    JobPersistentId = Guid.TryParse(parsedString[4], out var parsedJobId) ? parsedJobId : Guid.Empty,
                    RunPersistentId = Guid.TryParse(parsedString[5], out var parsedRunId) ? parsedRunId : Guid.Empty
                };

            if (parsedString[0].Equals(nameof(InterProcessPowershellProgressNotification)))
                return new InterProcessPowershellProgressNotification
                {
                    Sender = parsedString[1],
                    DatabaseId = Guid.TryParse(parsedString[2], out var parsedDbId) ? parsedDbId : Guid.Empty,
                    ScriptJobPersistentId = Guid.TryParse(parsedString[3], out var parsedRunScheduleId)
                        ? parsedRunScheduleId
                        : Guid.Empty,
                    ScriptJobRunPersistentId = Guid.TryParse(parsedString[4], out var parsedRunResultId)
                        ? parsedRunResultId
                        : Guid.Empty,
                    ProgressMessage = parsedString[5]
                };

            if (parsedString[0].Equals(nameof(InterProcessPowershellStateNotification)))
                return new InterProcessPowershellStateNotification
                {
                    Sender = parsedString[1],
                    DatabaseId = Guid.TryParse(parsedString[2], out var parsedDbId) ? parsedDbId : Guid.Empty,
                    ScriptJobPersistentId = Guid.TryParse(parsedString[3], out var parsedRunScheduleId)
                        ? parsedRunScheduleId
                        : Guid.Empty,
                    ScriptJobRunPersistentId = Guid.TryParse(parsedString[4], out var parsedRunResultId)
                        ? parsedRunResultId
                        : Guid.Empty,
                    State = Enum.Parse<PipelineState>(parsedString[5]),
                    ProgressMessage = parsedString[6]
                };
        }
        catch (Exception e)
        {
            return new InterProcessProcessingError { ErrorMessage = e.Message };
        }

        return new InterProcessProcessingError { ErrorMessage = "No processing occurred for this message?" };
    }

    public record InterProcessJobDataNotification
    {
        public Guid DatabaseId { get; set; }
        public Guid JobPersistentId { get; set; }
        public string? Sender { get; init; }
        public DataNotificationUpdateType UpdateType { get; init; }
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
        public Guid ScriptJobPersistentId { get; init; }
        public Guid ScriptJobRunPersistentId { get; init; }
        public string? Sender { get; init; }
        public PipelineState State { get; set; }
    }

    public record InterProcessProcessingError
    {
        public string ErrorMessage = string.Empty;
    }

    public record InterProcessRunDataNotification
    {
        public Guid DatabaseId { get; set; }
        public Guid JobPersistentId { get; set; }
        public Guid RunPersistentId { get; set; }
        public string? Sender { get; init; }
        public DataNotificationUpdateType UpdateType { get; init; }
    }
}