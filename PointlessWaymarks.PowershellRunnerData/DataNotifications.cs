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
        DataNotificationUpdateType updateType, int id)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"Data|{cleanedSender.Replace("|", " ")}|{(int)contentType}|{(int)updateType}|{id}");
    }

    public static void PublishPowershellProgressNotification(string sender, int scheduleId, int runId, string progress)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();
        var cleanedProgress = string.IsNullOrWhiteSpace(progress) ? "..." : progress.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"PowershellProgress|{cleanedSender.Replace("|", " ")}|{scheduleId}|{runId}|{cleanedProgress.Replace("|", " ")}");
    }

    public static void PublishPowershellStateNotification(string sender, int scheduleId, int runId, PipelineState state,
        string reason)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();
        var cleanedProgress = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();

        SendMessageQueue.Enqueue(
            $"PowershellState|{cleanedSender.Replace("|", " ")}|{scheduleId}|{runId}|{state}|{cleanedProgress.Replace("|", " ")}");
    }

    public static OneOf<InterProcessDataNotification, InterProcessPowershellProgressNotification,
            InterProcessPowershellStateNotification, InterProcessError>
        TranslateDataNotification(IReadOnlyList<byte>? received)
    {
        if (received == null || received.Count == 0)
            return new InterProcessError { ErrorMessage = "No Data" };

        try
        {
            var asString = Encoding.UTF8.GetString(received.ToArray());

            if (string.IsNullOrWhiteSpace(asString))
                return new InterProcessError { ErrorMessage = "Data is Blank" };

            var parsedString = asString.Split("|").ToList();

            if (!parsedString.Any()
                || !(parsedString.Count is 5 or 6)
                || !(parsedString[0].Equals("PowershellData") || parsedString[0].Equals("PowershellProgress") ||
                     parsedString[0].Equals("PowershellState"))
               )
                return new InterProcessError
                {
                    ErrorMessage = $"Data appears to be in the wrong format - {asString}"
                };

            if (parsedString[0].Equals("PowershellData"))
                return new InterProcessDataNotification
                {
                    Sender = parsedString[1],
                    ContentType = (DataNotificationContentType)int.Parse(parsedString[2]),
                    UpdateType = (DataNotificationUpdateType)int.Parse(parsedString[3]),
                    Id = int.TryParse(parsedString[4], out var parsedBatchId) ? parsedBatchId : -1
                };

            if (parsedString[0].Equals("PowershellProgress"))
                return new InterProcessPowershellProgressNotification
                {
                    Sender = parsedString[1],
                    ScriptJobId = int.TryParse(parsedString[2], out var parsedRunScheduleId)
                        ? parsedRunScheduleId
                        : -1,
                    ScriptJobRunId = int.TryParse(parsedString[3], out var parsedRunResultId) ? parsedRunResultId : -1,
                    ProgressMessage = parsedString[4]
                };

            if (parsedString[0].Equals("PowershellState"))
                return new InterProcessPowershellStateNotification
                {
                    Sender = parsedString[1],
                    ScriptJobId = int.TryParse(parsedString[2], out var parsedRunScheduleId)
                        ? parsedRunScheduleId
                        : -1,
                    ScriptJobRunId = int.TryParse(parsedString[3], out var parsedRunResultId) ? parsedRunResultId : -1,
                    State = Enum.Parse<PipelineState>(parsedString[4]),
                    ProgressMessage = parsedString[5]
                };
        }
        catch (Exception e)
        {
            return new InterProcessError { ErrorMessage = e.Message };
        }

        return new InterProcessError { ErrorMessage = "No processing occurred for this message?" };
    }

    public record InterProcessDataNotification
    {
        public DataNotificationContentType ContentType { get; init; }
        public int Id { get; init; }
        public string? Sender { get; init; }
        public DataNotificationUpdateType UpdateType { get; init; }
    }

    public record InterProcessError
    {
        public string ErrorMessage = string.Empty;
    }

    public record InterProcessPowershellProgressNotification
    {
        public string ProgressMessage { get; init; } = string.Empty;
        public int ScriptJobId { get; init; }
        public int ScriptJobRunId { get; init; }
        public string? Sender { get; init; }
    }

    public record InterProcessPowershellStateNotification
    {
        public string ProgressMessage { get; init; } = string.Empty;
        public int ScriptJobId { get; init; }
        public int ScriptJobRunId { get; init; }
        public string? Sender { get; init; }
        public PipelineState State { get; set; }
    }
}