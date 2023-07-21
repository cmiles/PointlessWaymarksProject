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
            RssItem => DataNotificationContentType.RssItem,
            RssFeed => DataNotificationContentType.RssFeed,
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
    public Guid JobPersistentId { get; set; }
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