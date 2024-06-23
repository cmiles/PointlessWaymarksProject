using System.Text;
using OneOf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.PowershellRunnerData.Models;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowershellRunnerData;

public static class DataNotifications
{
    public enum DataNotificationContentType
    {
        RunSchedule,
        RunResult,
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
            RunSchedule => DataNotificationContentType.RunSchedule,
            RunResult => DataNotificationContentType.RunResult,
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

    public static void PublishProgressNotification(string sender, int scheduleId, int runId, string progress)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();
        var cleanedProgress = string.IsNullOrWhiteSpace(progress) ? "..." : progress.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"Progress|{cleanedSender.Replace("|", " ")}|{scheduleId}|{runId}|{cleanedProgress.Replace("|", " ")}");
    }

    public static OneOf<InterProcessDataNotification, InterProcessProgressNotification, InterProcessError>
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
                || parsedString.Count is not 5
                || !(parsedString[0].Equals("Data") || parsedString[0].Equals("Progress"))
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
                    Id = int.TryParse(parsedString[4], out var parsedBatchId) ? parsedBatchId : -1
                };

            if (parsedString[0].Equals("Progress"))
                return new InterProcessProgressNotification
                {
                    Sender = parsedString[1],
                    RunScheduleId = int.TryParse(parsedString[2], out var parsedRunScheduleId)
                        ? parsedRunScheduleId
                        : -1,
                    RunResultId = int.TryParse(parsedString[3], out var parsedRunResultId) ? parsedRunResultId : -1,
                    ProgressMessage = parsedString[4]
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

    public record InterProcessProgressNotification
    {
        public string ProgressMessage { get; init; } = string.Empty;
        public int RunResultId { get; init; }
        public int RunScheduleId { get; init; }
        public string? Sender { get; init; }
    }
}