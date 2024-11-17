using System.Text;
using pinboard.net.Models;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsData;

public static class DataNotifications
{
    private static readonly string ChannelName =
        $"PointlessWaymarksCMS-{UserSettingsSingleton.CurrentSettings().SettingsId}";

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
            FileContent => DataNotificationContentType.File,
            GeoJsonContent => DataNotificationContentType.GeoJson,
            ImageContent => DataNotificationContentType.Image,
            LineContent => DataNotificationContentType.Line,
            LinkContent => DataNotificationContentType.Link,
            Note => DataNotificationContentType.Note,
            PhotoContent => DataNotificationContentType.Photo,
            PointContent => DataNotificationContentType.Point,
            PointDetail => DataNotificationContentType.PointDetail,
            PostContent => DataNotificationContentType.Post,
            TrailContent => DataNotificationContentType.Trail,
            VideoContent => DataNotificationContentType.Video,
            _ => DataNotificationContentType.Unknown
        };
    }

    public static void PublishDataNotification(string sender, DataNotificationContentType contentType,
        DataNotificationUpdateType updateType, List<Guid>? contentGuidList)
    {
        if (SuspendNotifications)
        {
            Log.ForContext("contentType", contentType).ForContext("updateType", updateType).ForContext("contentGuidList", contentGuidList).Debug("DataNotification Published while updates Suspended");
            return;
        }

        if (contentType != DataNotificationContentType.FileTransferScriptLog &&
            contentType != DataNotificationContentType.GenerationLog &&
            contentType != DataNotificationContentType.TagExclusion &&
            (contentGuidList == null || !contentGuidList.Any()))
        {
            Log.ForContext("contentType", contentType).ForContext("updateType", updateType).ForContext("contentGuidList", contentGuidList).Debug("DataNotification Published for Content but no ContentIds given");
            return;
        }

        var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

        var message =
            $"{cleanedSender.Replace("|", " ")}|{(int)contentType}|{(int)updateType}|{string.Join(",", contentGuidList ?? [])}";

        Log.ForContext("contentType", contentType).ForContext("updateType", updateType).ForContext("contentGuidList", contentGuidList).ForContext("message", message).Debug("DataNotification Published - Enqueue");

        SendMessageQueue.Enqueue(message);
    }

    public static InterProcessDataNotification TranslateDataNotification(IReadOnlyList<byte>? received)
    {
        if (received == null || received.Count == 0)
            return new InterProcessDataNotification { HasError = true, ErrorNote = "No Data" };

        try
        {
            var asString = Encoding.UTF8.GetString(received.ToArray());

            if (string.IsNullOrWhiteSpace(asString))
                return new InterProcessDataNotification { HasError = true, ErrorNote = "Data is Blank" };

            var parsedString = asString.Split("|").ToList();

            if (!parsedString.Any() || parsedString.Count != 4)
                return new InterProcessDataNotification
                {
                    HasError = true, ErrorNote = $"Data appears to be in the wrong format - {asString}"
                };

            return new InterProcessDataNotification
            {
                Sender = parsedString[0],
                ContentType = (DataNotificationContentType)int.Parse(parsedString[1]),
                UpdateType = (DataNotificationUpdateType)int.Parse(parsedString[2]),
                ContentIds = parsedString[3].Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse).ToList()
            };
        }
        catch (Exception e)
        {
            return new InterProcessDataNotification { HasError = true, ErrorNote = e.Message };
        }
    }
}

public record InterProcessDataNotification
{
    public List<Guid> ContentIds { get; init; } = [];
    public DataNotificationContentType ContentType { get; init; }
    public string? ErrorNote { get; init; }
    public bool HasError { get; init; }
    public string? Sender { get; init; }
    public DataNotificationUpdateType UpdateType { get; init; }
}

public enum DataNotificationUpdateType
{
    New,
    Update,
    Delete,
    LocalContent
}

public enum DataNotificationContentType
{
    File,
    GenerationLog,
    FileTransferScriptLog,
    GeoJson,
    Image,
    Line,
    Link,
    Map,
    MapElement,
    Note,
    Photo,
    Point,
    PointDetail,
    Post,
    Video,
    TagExclusion,
    Unknown,
    MapIcon,
    Snippet,
    Trail
}