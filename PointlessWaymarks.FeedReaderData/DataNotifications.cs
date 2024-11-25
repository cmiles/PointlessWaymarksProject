using System.Text;
using OneOf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData.Models;
using TinyIpc.Messaging;

namespace PointlessWaymarks.FeedReaderData;

public static class DataNotifications
{
    private static readonly string ChannelName =
        "PointlessWaymarksFeedReader";

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
            ReaderFeedItem => DataNotificationContentType.FeedItem,
            ReaderFeed => DataNotificationContentType.Feed,
            _ => DataNotificationContentType.Unknown
        };
    }

    public static void PublishDataNotification(string sender, DataNotificationContentType contentType,
        DataNotificationUpdateType updateType, List<Guid> ids)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"Data|{cleanedSender.Replace("|", " ")}|{(int)contentType}|{(int)updateType}|{string.Join(",", ids)}");
    }

    public static OneOf<InterProcessDataNotification, InterProcessError>
        TranslateDataNotification(string received)
    {
        if (string.IsNullOrWhiteSpace(received))
            return new InterProcessError { ErrorMessage = "No Data" };

        try
        {
            var parsedString = received.Split("|").ToList();

            if (!parsedString.Any()
                || parsedString.Count is not 5
                || !parsedString[0].Equals("Data"))
                return new InterProcessError
                {
                    ErrorMessage = $"Data appears to be in the wrong format - {received}"
                };

            return new InterProcessDataNotification
            {
                Sender = parsedString[1],
                ContentType = (DataNotificationContentType)int.Parse(parsedString[2]),
                UpdateType = (DataNotificationUpdateType)int.Parse(parsedString[3]),
                ContentIds = parsedString[4].Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse).ToList()
            };
        }
        catch (Exception e)
        {
            return new InterProcessError { ErrorMessage = e.Message };
        }
    }
}

public record InterProcessDataNotification
{
    public List<Guid> ContentIds { get; init; } = [];
    public DataNotificationContentType ContentType { get; init; }
    public string? Sender { get; init; }
    public DataNotificationUpdateType UpdateType { get; init; }
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
    FeedItem,
    Feed,
    SavedFeedItem,
    Unknown
}