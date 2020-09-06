using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pinboard.net.Models;
using PointlessWaymarksCmsData.Database.Models;
using TinyIpc.Messaging;

namespace PointlessWaymarksCmsData
{
    public static class DataNotifications
    {
        public static bool SuspendNotifications { get; set; }

        private static readonly TinyMessageBus DataNotificationTransmissionChannel = new TinyMessageBus("PointlessWaymarksDataNotificationChannel");

        public static TinyMessageBus NewDataNotificationChannel()
        {
            return new TinyMessageBus("PointlessWaymarksDataNotificationChannel");
        }

        public static DataNotificationContentType NotificationContentTypeFromContent(dynamic content)
        {
            switch (content)
            {
                case FileContent _: return DataNotificationContentType.File;
                case ImageContent _: return DataNotificationContentType.Image;
                case LinkContent _: return DataNotificationContentType.Link;
                case Note _: return DataNotificationContentType.Note;
                case PhotoContent _: return DataNotificationContentType.Photo;
                case PointContent _: return DataNotificationContentType.Point;
                case PointDetail _: return DataNotificationContentType.PointDetail;
                case PostContent _: return DataNotificationContentType.Post;
                default:
                    return DataNotificationContentType.Unknown;
            }
        }

        public static async Task PublishDataNotification(string sender, DataNotificationContentType contentType,
            DataNotificationUpdateType updateType, List<Guid> contentGuidList)
        {
            if (SuspendNotifications) return;

            if (contentGuidList == null || !contentGuidList.Any()) return;

            var cleanedSender = string.IsNullOrWhiteSpace(sender) ? "No Sender Specified" : sender.TrimNullToEmpty();

            await DataNotificationTransmissionChannel.PublishAsync(Encoding.UTF8.GetBytes(
                $"{cleanedSender.Replace("|", " ")}|{(int) contentType}|{(int) updateType}|{string.Join(",", contentGuidList)}"));
        }

        public static InterProcessDataNotification TranslateDataNotification(byte[] received)
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
                    ContentType = (DataNotificationContentType) (int.Parse(parsedString[1])),
                    UpdateType = (DataNotificationUpdateType) (int.Parse(parsedString[2])),
                    ContentIds = parsedString[3].Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => Guid.Parse(x)).ToList()
                };
            }
            catch (Exception e)
            {
                return new InterProcessDataNotification {HasError = true, ErrorNote = e.Message};
            }
        }
    }

    public class InterProcessDataNotification
    {
        public List<Guid> ContentIds { get; set; }
        public DataNotificationContentType ContentType { get; set; }
        public string ErrorNote { get; set; }
        public bool HasError { get; set; }
        public string Sender { get; set; }
        public DataNotificationUpdateType UpdateType { get; set; }
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
        Image,
        Link,
        Note,
        Photo,
        Point,
        PointDetail,
        Post,
        Unknown
    }
}