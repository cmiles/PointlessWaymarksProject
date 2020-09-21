using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Database.PointDetailDataModels;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsData.Spatial;

namespace PointlessWaymarksCmsData.Database
{
    public static class Db
    {
        public static async Task<ContentCommonShell> ContentCommonShellFromContentId(this PointlessWaymarksContext db,
            Guid contentId)
        {
            var possibleFile = await db.FileContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possibleFile != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleFile);

            var possibleImage = await db.ImageContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possibleImage != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleImage);

            var possibleLink = await db.LinkContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possibleLink != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleLink);

            var possiblePhoto = await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possiblePhoto != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possiblePhoto);

            var possiblePost = await db.PostContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possiblePost != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possiblePost);

            var possiblePoint = await db.PointContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possiblePoint != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possiblePoint);

            var possibleNote = await db.NoteContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleNote);
        }

        public static async Task<dynamic> ContentFromContentId(this PointlessWaymarksContext db, Guid contentId)
        {
            var possibleFile = await db.FileContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possibleFile != null) return possibleFile;

            var possibleImage = await db.ImageContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possibleImage != null) return possibleImage;

            var possibleLink = await db.LinkContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possibleLink != null) return possibleLink;

            var possiblePhoto = await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possiblePhoto != null) return possiblePhoto;

            var possiblePost = await db.PostContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possiblePost != null) return possiblePost;

            var possiblePoint = await db.PointContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            if (possiblePoint != null) return await PointAndPointDetails(contentId, db);

            var possibleNote = await db.NoteContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            return possibleNote;
        }

        public static async Task<List<dynamic>> ContentFromContentIds(this PointlessWaymarksContext db,
            List<Guid> contentIds)
        {
            if (contentIds == null || !contentIds.Any()) return new List<dynamic>();

            var returnList = new List<dynamic>();

            returnList.AddRange(db.FileContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.LinkContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.PhotoContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.PostContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.ImageContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.NoteContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(await PointsAndPointDetails(contentIds));

            return returnList;
        }

        private static DateTime ContentVersionDateTime()
        {
            var frozenNow = DateTime.Now.ToUniversalTime();
            return new DateTime(frozenNow.Year, frozenNow.Month, frozenNow.Day, frozenNow.Hour, frozenNow.Minute,
                frozenNow.Second, frozenNow.Kind);
        }
#pragma warning disable 1998
        public static async Task<PointlessWaymarksContext> Context()
#pragma warning restore 1998
        {
            var optionsBuilder = new DbContextOptionsBuilder<PointlessWaymarksContext>();
            var dbPath = UserSettingsSingleton.CurrentSettings().DatabaseFile;
            return new PointlessWaymarksContext(optionsBuilder.UseSqlite($"Data Source={dbPath}").Options);
        }

        /// <summary>
        ///     Uses reflection to Trim and Convert Nulls to Empty on all string properties and to truncate DateTimes to the
        ///     second.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toProcess"></param>
        public static void DefaultPropertyCleanup<T>(T toProcess)
        {
            StringHelpers.TrimNullToEmptyAllStringProperties(toProcess);
            DateTimeHelpers.TrimDateTimesToSeconds(toProcess);
            SpatialHelpers.RoundLatLongElevation(toProcess);
        }

        public static async Task<List<HistoricFileContent>> DeletedFileContent()
        {
            var db = await Context();

            var deletedContent = await (from h in db.HistoricFileContents
                where !db.FileContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync();

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricImageContent>> DeletedImageContent()
        {
            var db = await Context();

            var deletedContent = await (from h in db.HistoricImageContents
                where !db.ImageContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync();

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricLinkContent>> DeletedLinkContent()
        {
            var db = await Context();

            var deletedContent = await (from h in db.HistoricLinkContents
                where !db.LinkContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync();

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricNoteContent>> DeletedNoteContent()
        {
            var db = await Context();

            var deletedContent = await (from h in db.HistoricNoteContents
                where !db.NoteContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync();

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricPhotoContent>> DeletedPhotoContent()
        {
            var db = await Context();

            var deletedContent = await (from h in db.HistoricPhotoContents
                where !db.PhotoContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync();

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricPostContent>> DeletedPostContent()
        {
            var db = await Context();

            var deletedContent = await (from h in db.HistoricPostContents
                where !db.PostContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync();

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task DeleteFileContent(Guid contentId, IProgress<string> progress)
        {
            var context = await Context();

            var toHistoric = await context.FileContents.Where(x => x.ContentId == contentId).ToListAsync();

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricFileContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricFileContents.AddAsync(newHistoric);
                context.FileContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.File,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeleteImageContent(Guid contentId, IProgress<string> progress)
        {
            var context = await Context();

            var toHistoric = await context.ImageContents.Where(x => x.ContentId == contentId).ToListAsync();

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricImageContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricImageContents.AddAsync(newHistoric);
                context.ImageContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Image,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeleteLinkContent(Guid contentId, IProgress<string> progress)
        {
            var context = await Context();

            var toHistoric = await context.LinkContents.Where(x => x.ContentId == contentId).ToListAsync();

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricLinkContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricLinkContents.AddAsync(newHistoric);
                context.LinkContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Link,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeleteNoteContent(Guid contentId, IProgress<string> progress)
        {
            var context = await Context();

            var toHistoric = await context.NoteContents.Where(x => x.ContentId == contentId).ToListAsync();

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricNoteContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricNoteContents.AddAsync(newHistoric);
                context.NoteContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Note,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeletePhotoContent(Guid contentId, IProgress<string> progress)
        {
            var context = await Context();

            var toHistoric = await context.PhotoContents.Where(x => x.ContentId == contentId).ToListAsync();

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPhotoContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricPhotoContents.AddAsync(newHistoric);
                context.PhotoContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Photo,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeletePointContent(Guid contentId, IProgress<string> progress)
        {
            var context = await Context();

            var toHistoric = await context.PointContents.Where(x => x.ContentId == contentId).ToListAsync();

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPointContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricPointContents.AddAsync(newHistoric);
                context.PointContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Point,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeletePostContent(Guid contentId, IProgress<string> progress)
        {
            var context = await Context();

            var toHistoric = await context.PostContents.Where(x => x.ContentId == contentId).ToListAsync();

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPostContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricPostContents.AddAsync(newHistoric);
                context.PostContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Post,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task<List<string>> FolderNamesFromContent(dynamic content)
        {
            var db = await Context();

            switch (content)
            {
                case FileContent _:
                    return db.FileContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();
                case ImageContent _:
                    return db.ImageContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();
                case NoteContent _:
                    return db.NoteContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();
                case PhotoContent _:
                    return db.PhotoContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();
                case PointContent _:
                    return db.PointContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();
                case PostContent _:
                    return db.PostContents.Select(x => x.Folder).Distinct().OrderBy(x => x).ToList();
                default:
                    return new List<string>();
            }
        }

        public static async Task<List<HistoricPointDetail>> HistoricPointDetailsForPoint(Guid pointContentId,
            PointlessWaymarksContext db)
        {
            var detailLinks = await db.HistoricPointContentPointDetailLinks
                .Where(x => x.PointContentId == pointContentId).Select(x => x.PointContentId).Distinct().ToListAsync();

            var returnList = new List<HistoricPointDetail>();

            foreach (var loopDetailContentId in detailLinks)
                returnList.AddRange(db.HistoricPointDetails.Where(x => x.ContentId == loopDetailContentId)
                    .OrderByDescending(x => x.LastUpdatedOn).Take(10));

            return returnList;
        }

#pragma warning disable 1998
        public static async Task<EventLogContext> Log()
#pragma warning restore 1998
        {
            var optionsBuilder = new DbContextOptionsBuilder<EventLogContext>();
            var dbPath = Path.Combine(UserSettingsSingleton.CurrentSettings().LocalMediaArchive,
                "PointlessWaymarksCms-EventLog.db");
            return new EventLogContext(optionsBuilder.UseSqlite($"Data Source={dbPath}").Options);
        }

        public static async Task<List<IContentCommon>> MainFeedCommonContentAfter(DateTime after, int numberOfEntries)
        {
            var db = await Context();
            var fileContent = await db.FileContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var postContent = await db.PostContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var pointContent = await db.PointContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(pointContent)
                .Concat(noteContent).OrderBy(x => x.CreatedOn).Take(numberOfEntries).ToList();
        }

        public static async Task<List<IContentCommon>> MainFeedCommonContentBefore(DateTime before, int numberOfEntries)
        {
            var db = await Context();
            var fileContent = await db.FileContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var postContent = await db.PostContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var pointContent = await db.PointContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(pointContent)
                .Concat(noteContent).OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).ToList();
        }

        public static async Task<List<dynamic>> MainFeedDynamicContentAfter(DateTime after, int numberOfEntries)
        {
            var db = await Context();
            var fileContent = await db.FileContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderBy(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderBy(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderBy(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var postContent = await db.PostContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderBy(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var pointContent = await db.PointContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderBy(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < after)
                .OrderBy(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(pointContent)
                .Concat(noteContent).OrderBy(x => x.CreatedOn).Take(numberOfEntries).ToList();
        }

        public static async Task<List<dynamic>> MainFeedDynamicContentBefore(DateTime before, int numberOfEntries)
        {
            var db = await Context();
            var fileContent = await db.FileContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var postContent = await db.PostContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var pointContent = await db.PointContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(pointContent)
                .Concat(noteContent).OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).ToList();
        }

        public static async Task<List<IContentCommon>> MainFeedRecentCommonContent(int topNumberOfEntries)
        {
            var db = await Context();
            var fileContent = await db.FileContents.Where(x => x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(topNumberOfEntries).Cast<IContentCommon>().ToListAsync();
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed)
                .OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).Cast<IContentCommon>().ToListAsync();
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed)
                .OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).Cast<IContentCommon>().ToListAsync();
            var postContent = await db.PostContents.Where(x => x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(topNumberOfEntries).Cast<IContentCommon>().ToListAsync();
            var pointContent = await db.PointContents.Where(x => x.ShowInMainSiteFeed)
                .OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).Cast<IContentCommon>().ToListAsync();
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(topNumberOfEntries).Cast<IContentCommon>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(pointContent)
                .Concat(noteContent).OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).ToList();
        }

        public static async Task<List<dynamic>> MainFeedRecentDynamicContent(int topNumberOfEntries)
        {
            var db = await Context();
            var fileContent = await db.FileContents.Where(x => x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(topNumberOfEntries).Cast<dynamic>().ToListAsync();
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed)
                .OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).Cast<dynamic>().ToListAsync();
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed)
                .OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).Cast<dynamic>().ToListAsync();
            var postContent = await db.PostContents.Where(x => x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(topNumberOfEntries).Cast<dynamic>().ToListAsync();
            var pointContent = await db.PointContents.Where(x => x.ShowInMainSiteFeed)
                .OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).Cast<dynamic>().ToListAsync();
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(topNumberOfEntries).Cast<dynamic>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(pointContent)
                .Concat(noteContent).OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).ToList();
        }

        public static Guid? MainImageContentIdIfPresent(dynamic content)
        {
            return content switch
            {
                FileContent f => f.MainPicture,
                PostContent p => p.MainPicture,
                _ => null
            };
        }

        public static List<(string tag, List<object> contentObjects)> ParseToTagSlugsAndContentList(
            List<(string tag, List<object> contentObjects)> list, List<ITag> toAdd, IProgress<string> progress)
        {
            list ??= new List<(string tag, List<object> contentObjects)>();

            if (toAdd == null) return list;

            var i = 0;

            toAdd.ForEach(x =>
            {
                i++;

                if (i % 20 == 0) progress?.Report($"Processing Tag Content - {i} of {toAdd.Count}");
                ParseToTagSlugsAndContentList(list, x);
            });

            return list;
        }

        public static List<(string tag, List<object> contentObjects)> ParseToTagSlugsAndContentList(
            List<(string tag, List<object> contentObjects)> list, ITag toAdd)
        {
            list ??= new List<(string tag, List<object> contentObjects)>();

            var tags = TagListParseToSlugs(toAdd, true);

            foreach (var loopTags in tags)
            {
                var existingEntry = list.SingleOrDefault(x => x.tag.ToLower() == loopTags.ToLower());
                if (string.IsNullOrWhiteSpace(existingEntry.tag))
                    list.Add((loopTags.ToLower(), new List<object> {toAdd}));
                else
                    existingEntry.contentObjects.Add(toAdd);
            }

            return list;
        }

        public static async Task<PointContentDto> PointAndPointDetails(Guid pointContentId)
        {
            var db = await Context();

            return await PointAndPointDetails(pointContentId, db);
        }

        public static async Task<PointContentDto> PointAndPointDetails(Guid pointContentId, PointlessWaymarksContext db)
        {
            var point = await db.PointContents.SingleAsync(x => x.ContentId == pointContentId);
            var detailLinks = await db.PointContentPointDetailLinks.Where(x => x.PointContentId == pointContentId)
                .Select(x => x.PointDetailContentId).ToListAsync();
            var details = await db.PointDetails.Where(x => detailLinks.Contains(x.ContentId)).ToListAsync();

            var toReturn = new PointContentDto();
            toReturn.InjectFrom(point);
            toReturn.PointDetails = details;

            return toReturn;
        }

        public static PointContentDto PointContentDtoFromPointContentAndDetails(PointContent content,
            List<PointDetail> details)
        {
            var toReturn = new PointContentDto();

            if (content != null) toReturn.InjectFrom(content);

            details ??= new List<PointDetail>();

            toReturn.PointDetails = details;

            return toReturn;
        }

        public static (PointContent content, List<PointDetail> details) PointContentDtoToPointContentAndDetails(
            PointContentDto dto)
        {
            var toSave = (PointContent) new PointContent().InjectFrom(dto);
            var relatedDetails = dto.PointDetails ?? new List<PointDetail>();

            return (toSave, relatedDetails);
        }

        public static IPointDetailData PointDetailDataFromIdentifierAndJson(string dataIdentifier, string json)
        {
            return dataIdentifier switch
            {
                "Campground" => JsonSerializer.Deserialize<Campground>(json),
                "Feature" => JsonSerializer.Deserialize<Feature>(json),
                "Parking" => JsonSerializer.Deserialize<Parking>(json),
                "Peak" => JsonSerializer.Deserialize<Peak>(json),
                "Restroom" => JsonSerializer.Deserialize<Restroom>(json),
                "Trail Junction" => JsonSerializer.Deserialize<TrailJunction>(json),
                _ => null
            };
        }

        public static bool PointDetailDataTypeIsValid(string dataType)
        {
            var pointDetailTypes = from type in typeof(Db).Assembly.GetTypes()
                where typeof(IPointDetailData).IsAssignableFrom(type) && !type.IsInterface
                select type;

            foreach (var loopTypes in pointDetailTypes)
            {
                var typeExample = (IPointDetailData) Activator.CreateInstance(loopTypes);

                if (typeExample == null) continue;

                if (typeExample.DataTypeIdentifier == dataType) return true;
            }

            return false;
        }

        public static async Task<List<PointDetail>> PointDetailsForPoint(Guid pointContentId,
            PointlessWaymarksContext db)
        {
            var detailLinks = await db.PointContentPointDetailLinks.Where(x => x.PointContentId == pointContentId)
                .Select(x => x.PointDetailContentId).ToListAsync();
            var details = await db.PointDetails.Where(x => detailLinks.Contains(x.ContentId)).ToListAsync();

            return details;
        }

        public static async Task<List<PointContentDto>> PointsAndPointDetails(List<Guid> pointContentId)
        {
            var db = await Context();

            var idChunks = pointContentId.Partition(250);

            var returnList = new List<PointContentDto>();

            foreach (var loopChunk in idChunks)
            {
                var contents = await db.PointContents.Where(x => loopChunk.Contains(x.ContentId)).ToListAsync();

                foreach (var loopContent in contents)
                {
                    var details = await PointDetailsForPoint(loopContent.ContentId, db);
                    var toAdd = new PointContentDto();
                    toAdd.InjectFrom(loopContent);
                    toAdd.PointDetails = details;

                    returnList.Add(toAdd);
                }
            }

            return returnList;
        }

        public static async Task SaveFileContent(FileContent toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.FileContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricFileContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricFileContents.AddAsync(newHistoric);
                context.FileContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            await context.FileContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.File,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SaveImageContent(ImageContent toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.ImageContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricImageContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricImageContents.AddAsync(newHistoric);
                context.ImageContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.ImageContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            toSave.MainPicture = toSave.ContentId;

            await context.SaveChangesAsync(true);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Image,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SaveLinkContent(LinkContent toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.LinkContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricLinkContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricLinkContents.AddAsync(newHistoric);
                context.LinkContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.LinkContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Link,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SaveNoteContent(NoteContent toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.NoteContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricNoteContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricNoteContents.AddAsync(newHistoric);
                context.NoteContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.NoteContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Note,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SavePhotoContent(PhotoContent toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.PhotoContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPhotoContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricPhotoContents.AddAsync(newHistoric);
                context.PhotoContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.PhotoContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            toSave.MainPicture = toSave.ContentId;

            await context.SaveChangesAsync(true);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Photo,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task<PointContentDto> SavePointContent(PointContentDto toSaveDto)
        {
            if (toSaveDto == null) return null;

            var (toSave, relatedDetails) = PointContentDtoToPointContentAndDetails(toSaveDto);

            var context = await Context();

            var toHistoric = await context.PointContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPointContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricPointContents.AddAsync(newHistoric);
                context.PointContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            await context.PointContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            foreach (var loopDetail in relatedDetails) await SavePointDetailContent(loopDetail);

            var relatedDetailContentIds = relatedDetails.Select(x => x.ContentId).ToList();

            var invalidLinks = await context.PointContentPointDetailLinks.Where(x =>
                    x.PointContentId == toSave.ContentId && !relatedDetailContentIds.Contains(x.PointDetailContentId))
                .ToListAsync();

            foreach (var loopInvalids in invalidLinks)
            {
                var relatedDetailEntries =
                    context.PointDetails.Where(x => x.ContentId == loopInvalids.PointDetailContentId);

                foreach (var loopRelatedDetails in relatedDetailEntries)
                {
                    var newHistoricDetail = new HistoricPointDetail();
                    newHistoricDetail.InjectFrom(loopRelatedDetails);
                    newHistoricDetail.Id = 0;
                    await context.HistoricPointDetails.AddAsync(newHistoricDetail);
                    context.PointDetails.Remove(loopRelatedDetails);
                }

                var newHistoricLink = new HistoricPointContentPointDetailLink();
                newHistoricLink.InjectFrom(loopInvalids);
                newHistoricLink.Id = 0;
                await context.HistoricPointContentPointDetailLinks.AddAsync(newHistoricLink);
                context.PointContentPointDetailLinks.Remove(loopInvalids);
            }

            await context.SaveChangesAsync(true);

            foreach (var loopDetails in relatedDetails)
            {
                var possibleLink = context.PointContentPointDetailLinks.SingleOrDefault(x =>
                    x.PointContentId == toSave.ContentId && x.PointDetailContentId == toSave.ContentId);

                if (possibleLink == null)
                {
                    possibleLink = new PointContentPointDetailLink
                    {
                        PointContentId = toSave.ContentId, PointDetailContentId = loopDetails.ContentId
                    };

                    await context.PointContentPointDetailLinks.AddAsync(possibleLink);
                }

                possibleLink.ContentVersion = ContentVersionDateTime();

                await context.SaveChangesAsync();
            }

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Point,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                toSave.ContentId.AsList());

            return await PointAndPointDetails(toSaveDto.ContentId);
        }

        public static async Task SavePointDetailContent(PointContentPointDetailLink toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var existing = await context.PointContentPointDetailLinks.AnyAsync(x =>
                x.PointContentId == toSave.PointContentId && x.PointDetailContentId == toSave.PointDetailContentId);

            if (existing) return;

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.PointContentPointDetailLinks.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Point,
                DataNotificationUpdateType.Update, new List<Guid> {toSave.PointContentId});
            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
                DataNotificationUpdateType.Update, new List<Guid> {toSave.PointDetailContentId});
        }

        public static async Task SavePointDetailContent(PointDetail toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.PointDetails.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPointDetail();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                await context.HistoricPointDetails.AddAsync(newHistoric);
                context.PointDetails.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;

            toSave.ContentVersion = ContentVersionDateTime();

            await context.PointDetails.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SavePostContent(PostContent toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.PostContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPostContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricPostContents.AddAsync(newHistoric);
                context.PostContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            await context.PostContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Post,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static string TagListCleanup(string tags)
        {
            return string.IsNullOrWhiteSpace(tags) ? string.Empty : TagListJoin(TagListParse(tags));
        }

        public static List<string> TagListCleanup(List<string> listToClean)
        {
            if (listToClean == null || !listToClean.Any()) return new List<string>();

            return listToClean.Select(TagListItemCleanup).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        }

        /// <summary>
        ///     Use to clean up a single tag - trims and removes inner multi-space
        /// </summary>
        /// <param name="toClean"></param>
        /// <returns></returns>
        public static string TagListItemCleanup(string toClean)
        {
            if (string.IsNullOrWhiteSpace(toClean)) return string.Empty;

            return Regex.Replace(SlugUtility.CreateSpacedString(true, toClean, 200), @"\s+", " ").TrimNullToEmpty()
                .ToLower();
        }

        public static string TagListJoin(List<string> tagList)
        {
            if (tagList == null) return string.Empty;
            if (tagList.Count < 1) return string.Empty;

            var cleanedList = tagList.Select(TagListItemCleanup).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()
                .OrderBy(x => x).ToList();

            return string.Join(",", cleanedList);
        }

        public static string TagListJoinAsSlugs(List<string> tagList, bool removeExcludedTags)
        {
            if (tagList == null) return string.Empty;
            if (tagList.Count < 1) return string.Empty;

            var excludedTags = new List<string>();

            if (removeExcludedTags)
            {
                var db = Context().Result;
                excludedTags = db.TagExclusions.ToList().Select(x => SlugUtility.Create(true, x.Tag, 200)).ToList();
            }

            var cleanedList = tagList.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => SlugUtility.Create(true, x.Trim(), 200)).Distinct().Except(excludedTags).OrderBy(x => x)
                .ToList();

            return string.Join(",", cleanedList);
        }

        public static List<string> TagListParse(string rawTagString)
        {
            if (rawTagString == null) return new List<string>();
            if (string.IsNullOrWhiteSpace(rawTagString)) return new List<string>();

            return rawTagString.Split(",").Select(TagListItemCleanup).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct().OrderBy(x => x).ToList();
        }

        public static List<string> TagListParseToSlugs(string rawTagString, bool removeExcludedTags)
        {
            if (rawTagString == null) return new List<string>();
            if (string.IsNullOrWhiteSpace(rawTagString)) return new List<string>();

            var excludedTags = new List<string>();

            if (removeExcludedTags)
            {
                var db = Context().Result;
                excludedTags = db.TagExclusions.ToList().Select(x => SlugUtility.Create(true, x.Tag, 200)).ToList();
            }

            return rawTagString.Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
                .Select(x => SlugUtility.Create(true, x, 200)).Distinct().Where(x => !excludedTags.Contains(x))
                .OrderBy(x => x).ToList();
        }

        public static List<string> TagListParseToSlugs(ITag tag, bool removeExcludedTags)
        {
            if (tag == null) return new List<string>();
            if (string.IsNullOrWhiteSpace(tag.Tags)) return new List<string>();

            return TagListParseToSlugs(tag.Tags, removeExcludedTags);
        }

        public static async Task<List<(string tag, List<dynamic> contentObjects)>> TagSlugsAndContentList(
            bool includePagesExcludedFromSearch, IProgress<string> progress)
        {
            var db = await Context();

            progress?.Report("Starting Parse of Tag Content");

            var returnList = new List<(string tag, List<object> contentObjects)>();

            progress?.Report("Process File Content Tags");
            ParseToTagSlugsAndContentList(returnList, (await db.FileContents.ToListAsync()).Cast<ITag>().ToList(),
                progress);

            progress?.Report("Process Photo Content Tags");
            ParseToTagSlugsAndContentList(returnList, (await db.PhotoContents.ToListAsync()).Cast<ITag>().ToList(),
                progress);

            progress?.Report("Process Image Content Tags");
            ParseToTagSlugsAndContentList(returnList,
                includePagesExcludedFromSearch
                    ? (await db.ImageContents.ToListAsync()).Cast<ITag>().ToList()
                    : (await db.ImageContents.Where(x => x.ShowInSearch).ToListAsync()).Cast<ITag>().ToList(),
                progress);

            progress?.Report("Process Point Content Tags");
            ParseToTagSlugsAndContentList(returnList, (await db.PointContents.ToListAsync()).Cast<ITag>().ToList(),
                progress);

            progress?.Report("Process Post Content Tags");
            ParseToTagSlugsAndContentList(returnList, (await db.PostContents.ToListAsync()).Cast<ITag>().ToList(),
                progress);

            progress?.Report("Process Note Content Tags");
            ParseToTagSlugsAndContentList(returnList, (await db.NoteContents.ToListAsync()).Cast<ITag>().ToList(),
                progress);

            progress?.Report("Process Link Content Tags");
            ParseToTagSlugsAndContentList(returnList, (await db.LinkContents.ToListAsync()).Cast<ITag>().ToList(),
                progress);

            progress?.Report("Finished Parsing Tag Content");

            return returnList;
        }
    }
}