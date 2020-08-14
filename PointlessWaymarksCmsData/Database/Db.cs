using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Html.CommonHtml;

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

            var possibleNote = await db.NoteContents.SingleOrDefaultAsync(x => x.ContentId == contentId);
            return possibleNote;
        }

        public static List<dynamic> ContentFromContentIds(this PointlessWaymarksContext db, List<Guid> contentIds)
        {
            if (contentIds == null || !contentIds.Any()) return new List<dynamic>();

            var returnList = new List<dynamic>();

            returnList.AddRange(db.FileContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.LinkContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.PhotoContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.PostContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.ImageContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.NoteContents.Where(x => contentIds.Contains(x.ContentId)));

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
                await context.HistoricFileContents.AddAsync(newHistoric);
                context.FileContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.File,
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
                await context.HistoricImageContents.AddAsync(newHistoric);
                context.ImageContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Image,
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
                await context.HistoricLinkContents.AddAsync(newHistoric);
                context.LinkContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Link,
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
                await context.HistoricNoteContents.AddAsync(newHistoric);
                context.NoteContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Note,
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
                await context.HistoricPhotoContents.AddAsync(newHistoric);
                context.PhotoContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Photo,
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
                await context.HistoricPostContents.AddAsync(newHistoric);
                context.PostContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Post,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
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
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn > after)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(noteContent)
                .OrderBy(x => x.CreatedOn).Take(numberOfEntries).ToList();
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
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(noteContent)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).ToList();
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
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < after)
                .OrderBy(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(noteContent)
                .OrderBy(x => x.CreatedOn).Take(numberOfEntries).ToList();
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
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && x.CreatedOn < before)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).Cast<dynamic>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(noteContent)
                .OrderByDescending(x => x.CreatedOn).Take(numberOfEntries).ToList();
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
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(topNumberOfEntries).Cast<IContentCommon>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(noteContent)
                .OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).ToList();
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
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed).OrderByDescending(x => x.CreatedOn)
                .Take(topNumberOfEntries).Cast<dynamic>().ToListAsync();

            return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(noteContent)
                .OrderByDescending(x => x.CreatedOn).Take(topNumberOfEntries).ToList();
        }

        public static Guid? MainImageContentIdIfPresent(dynamic content)
        {
            return content switch
            {
                FileContent f => content.MainImage,
                PostContent p => content.MainImage,
                _ => null
            };
        }

        public static List<(string tag, List<object> contentObjects)> ParseToTagAndContentList(
            List<(string tag, List<object> contentObjects)> list, List<ITag> toAdd, IProgress<string> progress)
        {
            list ??= new List<(string tag, List<object> contentObjects)>();

            if (toAdd == null) return list;

            var i = 0;

            toAdd.ForEach(x =>
            {
                i++;

                if (i % 20 == 0) progress?.Report($"Processing Tag Content - {i} of {toAdd.Count}");
                ParseToTagAndContentList(list, x);
            });

            return list;
        }

        public static List<(string tag, List<object> contentObjects)> ParseToTagAndContentList(
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
                await context.HistoricFileContents.AddAsync(newHistoric);
                context.FileContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            await context.FileContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            if (isUpdate)
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.File,
                    DataNotificationUpdateType.Update, new List<Guid> {toSave.ContentId});
            else
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.File,
                    DataNotificationUpdateType.New, new List<Guid> {toSave.ContentId});
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
                await context.HistoricImageContents.AddAsync(newHistoric);
                context.ImageContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.ImageContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            toSave.MainPicture = toSave.ContentId;

            await context.SaveChangesAsync(true);

            if (isUpdate)
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Image,
                    DataNotificationUpdateType.Update, new List<Guid> {toSave.ContentId});
            else
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Image,
                    DataNotificationUpdateType.New, new List<Guid> {toSave.ContentId});
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
                await context.HistoricLinkContents.AddAsync(newHistoric);
                context.LinkContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.LinkContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            if (isUpdate)
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Link,
                    DataNotificationUpdateType.Update, new List<Guid> {toSave.ContentId});
            else
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Link,
                    DataNotificationUpdateType.New, new List<Guid> {toSave.ContentId});
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
                await context.HistoricNoteContents.AddAsync(newHistoric);
                context.NoteContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.NoteContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            if (isUpdate)
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Note,
                    DataNotificationUpdateType.Update, new List<Guid> {toSave.ContentId});
            else
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Note,
                    DataNotificationUpdateType.New, new List<Guid> {toSave.ContentId});
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
                await context.HistoricPhotoContents.AddAsync(newHistoric);
                context.PhotoContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.PhotoContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            toSave.MainPicture = toSave.ContentId;

            await context.SaveChangesAsync(true);

            if (isUpdate)
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Photo,
                    DataNotificationUpdateType.Update, new List<Guid> {toSave.ContentId});
            else
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Photo,
                    DataNotificationUpdateType.New, new List<Guid> {toSave.ContentId});
        }

        public static async Task SavePointContent(PointContent toSave, List<PointDetail> relatedDetails)
        {
            if (toSave == null) return;
            relatedDetails ??= new List<PointDetail>();

            var context = await Context();

            var toHistoric = await context.PointContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPointContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
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
                var newHistoric = new HistoricPointContentPointDetailLink();
                newHistoric.InjectFrom(loopInvalids);
                newHistoric.Id = 0;
                await context.HistoricPointContentPointDetailLinks.AddAsync(newHistoric);
                context.PointContentPointDetailLinks.Remove(loopInvalids);
            }

            await context.SaveChangesAsync(true);

            if (isUpdate)
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Point,
                    DataNotificationUpdateType.Update, new List<Guid> {toSave.ContentId});
            else
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Point,
                    DataNotificationUpdateType.New, new List<Guid> {toSave.ContentId});
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

            await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Point,
                DataNotificationUpdateType.Update, new List<Guid> {toSave.PointContentId});
            await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
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
                await context.HistoricPointDetails.AddAsync(newHistoric);
                context.PointDetails.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.PointDetails.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            if (isUpdate)
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
                    DataNotificationUpdateType.Update, new List<Guid> {toSave.ContentId});
            else
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
                    DataNotificationUpdateType.New, new List<Guid> {toSave.ContentId});
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
                await context.HistoricPostContents.AddAsync(newHistoric);
                context.PostContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            await context.PostContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            if (isUpdate)
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Post,
                    DataNotificationUpdateType.Update, new List<Guid> {toSave.ContentId});
            else
                await DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Post,
                    DataNotificationUpdateType.New, new List<Guid> {toSave.ContentId});
        }

        public static async Task<List<(string tag, List<dynamic> contentObjects)>> TagAndContentList(
            bool includePagesExcludedFromSearch, IProgress<string> progress)
        {
            var db = await Context();

            progress?.Report("Starting Parse of Tag Content");

            var returnList = new List<(string tag, List<object> contentObjects)>();

            progress?.Report("Process File Content Tags");
            ParseToTagAndContentList(returnList, (await db.FileContents.ToListAsync()).Cast<ITag>().ToList(), progress);

            progress?.Report("Process Photo Content Tags");
            ParseToTagAndContentList(returnList, (await db.PhotoContents.ToListAsync()).Cast<ITag>().ToList(),
                progress);

            progress?.Report("Process Image Content Tags");
            ParseToTagAndContentList(returnList,
                includePagesExcludedFromSearch
                    ? (await db.ImageContents.ToListAsync()).Cast<ITag>().ToList()
                    : (await db.ImageContents.Where(x => x.ShowInSearch).ToListAsync()).Cast<ITag>().ToList(),
                progress);

            progress?.Report("Process Post Content Tags");
            ParseToTagAndContentList(returnList, (await db.PostContents.ToListAsync()).Cast<ITag>().ToList(), progress);

            progress?.Report("Process Note Content Tags");
            ParseToTagAndContentList(returnList, (await db.NoteContents.ToListAsync()).Cast<ITag>().ToList(), progress);

            progress?.Report("Process Link Content Tags");
            ParseToTagAndContentList(returnList, (await db.LinkContents.ToListAsync()).Cast<ITag>().ToList(), progress);

            progress?.Report("Finished Parsing Tag Content");

            return returnList;
        }

        public static string TagListCleanup(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags)) return string.Empty;

            return TagListJoin(TagListParse(tags));
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
    }
}