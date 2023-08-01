using System.Text;
using OneOf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.RssReaderData.Models;
using TinyIpc.Messaging;

namespace PointlessWaymarks.RssReaderData;

public static class DataNotifications
{
    private static readonly string ChannelName =
        "PointlessWaymarksRss";

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
            FeedItem => DataNotificationContentType.RssItem,
            Feed => DataNotificationContentType.RssFeed,
            _ => DataNotificationContentType.Unknown
        };
    }

    public static void PublishDataNotification(string sender, DataNotificationContentType contentType,
        DataNotificationUpdateType updateType, List<Guid> ids)
    {
        if (SuspendNotifications) return;

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

        SendMessageQueue.Enqueue(
            $"Data|{cleanedSender.Replace("|", " ")}|{(int)contentType}|{(int)updateType}|{string.Join(",", ids ?? new List<Guid>())}");
    }

    public static OneOf<InterProcessDataNotification, InterProcessError>
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
                || !parsedString[0].Equals("Data"))
                return new InterProcessError
                {
                    ErrorMessage = $"Data appears to be in the wrong format - {asString}"
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

        return new InterProcessError { ErrorMessage = "No processing occured for this message?" };
    }
}

public record InterProcessDataNotification
{
    public List<Guid> ContentIds { get; set; }
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
    RssItem,
    RssFeed,
    Unknown
}