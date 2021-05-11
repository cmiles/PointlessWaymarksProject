using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pinboard.net.Models;
using PointlessWaymarks.CmsData.Database.Models;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsData
{
    public static class DataNotifications
    {
        private static readonly TinyMessageBus DataNotificationTransmissionChannel =
            new("PointlessWaymarksDataNotificationChannel");

        private static readonly WorkQueue<string> SendMessageQueue = new()
        {
            Processor = async x => await DataNotificationTransmissionChannel.PublishAsync(Encoding.UTF8.GetBytes(x))
        };

        public static bool SuspendNotifications { get; set; }

        public static TinyMessageBus NewDataNotificationChannel()
        {
            return new("PointlessWaymarksDataNotificationChannel");
        }

        public static DataNotificationContentType NotificationContentTypeFromContent(dynamic content)
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
                _ => DataNotificationContentType.Unknown
            };
        }

        public static void PublishDataNotification(string sender, DataNotificationContentType contentType,
            DataNotificationUpdateType updateType, List<Guid>? contentGuidList)
        {
            if (SuspendNotifications) return;

            if (contentType != DataNotificationContentType.FileTransferScriptLog &&
                contentType != DataNotificationContentType.GenerationLog &&
                (contentGuidList == null || !contentGuidList.Any())) return;

            var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

            SendMessageQueue.Enqueue(
                $"{cleanedSender.Replace("|", " ")}|{(int) contentType}|{(int) updateType}|{string.Join(",", contentGuidList ?? new List<Guid>())}");
        }

        public static InterProcessDataNotification TranslateDataNotification(byte[]? received)
        {
            if (received == null || received.Length == 0)
                return new InterProcessDataNotification {HasError = true, ErrorNote = "No Data"};

            try
            {
                var asString = Encoding.UTF8.GetString(received);

                if (string.IsNullOrWhiteSpace(asString))
                    return new InterProcessDataNotification {HasError = true, ErrorNote = "Data is Blank"};

                var parsedString = asString.Split("|").ToList();

                if (!parsedString.Any() || parsedString.Count != 4)
                    return new InterProcessDataNotification
                    {
                        HasError = true, ErrorNote = $"Data appears to be in the wrong format - {asString}"
                    };

                return new InterProcessDataNotification
                {
                    Sender = parsedString[0],
                    ContentType = (DataNotificationContentType) int.Parse(parsedString[1]),
                    UpdateType = (DataNotificationUpdateType) int.Parse(parsedString[2]),
                    ContentIds = parsedString[3].Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(Guid.Parse).ToList()
                };
            }
            catch (Exception e)
            {
                return new InterProcessDataNotification {HasError = true, ErrorNote = e.Message};
            }
        }
    }

    public record InterProcessDataNotification
    {
        public List<Guid>? ContentIds { get; init; }
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
        Unknown
    }
}