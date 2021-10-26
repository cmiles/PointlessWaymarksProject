﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;
using PointlessWaymarks.CmsData.Spatial;

namespace PointlessWaymarks.CmsData.Database
{
    public static class Db
    {
        /// <summary>
        ///     Returns a ContentCommonShell based on the ContentId - all content that types are included but because of the
        ///     transformation to a concrete ContentCommonShell not all data will be available.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="contentId"></param>
        /// <returns></returns>
        public static async Task<ContentCommonShell?> ContentCommonShellFromContentId(this PointlessWaymarksContext db,
            Guid contentId)
        {
            //!Content Type List!!
            var possibleFile = await db.FileContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleFile != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleFile);

            var possibleGeoJson = await db.GeoJsonContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleGeoJson != null)
                return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleGeoJson);

            var possibleImage = await db.ImageContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleImage != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleImage);

            var possibleLine = await db.LineContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleLine != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleLine);

            var possibleLink = await db.LinkContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleLink != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleLink);

            var possibleNote = await db.NoteContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleNote != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possibleNote);

            var possiblePhoto = await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possiblePhoto != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possiblePhoto);

            var possiblePoint = await db.PointContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possiblePoint != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possiblePoint);

            var possiblePost = await db.PostContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possiblePost != null) return (ContentCommonShell) new ContentCommonShell().InjectFrom(possiblePost);

            return null;
        }

        public static async Task<List<ContentCommonShell>> ContentCommonShellFromContentIds(
            this PointlessWaymarksContext db, List<Guid>? contentIds)
        {
            if (contentIds == null || !contentIds.Any()) return new List<ContentCommonShell>();

            var returnList = new List<ContentCommonShell>();

            foreach (var loopIds in contentIds)
            {
                var toAdd = await ContentCommonShellFromContentId(db, loopIds).ConfigureAwait(false);

                if (toAdd != null) returnList.Add(toAdd);
            }

            return returnList;
        }

        public static async Task<dynamic?> ContentFromContentId(this PointlessWaymarksContext db, Guid contentId)
        {
            //!!Content Type List!!
            var possibleFile = await db.FileContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleFile != null) return possibleFile;

            var possibleGeoJson = await db.GeoJsonContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleGeoJson != null) return possibleGeoJson;

            var possibleImage = await db.ImageContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleImage != null) return possibleImage;

            var possibleLine = await db.LineContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleLine != null) return possibleLine;

            var possibleLink = await db.LinkContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleLink != null) return possibleLink;

            var possibleNote = await db.NoteContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possibleNote != null) return possibleNote;

            var possiblePhoto = await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possiblePhoto != null) return possiblePhoto;

            var possiblePoint = await db.PointContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possiblePoint != null) return await PointAndPointDetails(contentId, db).ConfigureAwait(false);

            var possiblePost = await db.PostContents.SingleOrDefaultAsync(x => x.ContentId == contentId).ConfigureAwait(false);
            if (possiblePost != null) return possiblePost;

            return null;
        }

        public static async Task<List<dynamic>> ContentFromContentIds(this PointlessWaymarksContext db,
            List<Guid> contentIds)
        {
            if (!contentIds.Any()) return new List<dynamic>();

            var returnList = new List<dynamic>();

            returnList.AddRange(db.FileContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.GeoJsonContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.ImageContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.LineContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.LinkContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.NoteContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(db.PhotoContents.Where(x => contentIds.Contains(x.ContentId)));
            returnList.AddRange(await PointsAndPointDetails(contentIds).ConfigureAwait(false));
            returnList.AddRange(db.PostContents.Where(x => contentIds.Contains(x.ContentId)));

            return returnList;
        }

        public static async Task<bool> ContentIdIsSpatialContentInDatabase(Guid toValidate)
        {
            var db = await Context().ConfigureAwait(false);

            if (db.PointContents.Any(x => x.ContentId == toValidate)) return true;
            if (db.GeoJsonContents.Any(x => x.ContentId == toValidate)) return true;
            if (db.LineContents.Any(x => x.ContentId == toValidate)) return true;

            return false;
        }

        public static string ContentTypeString(dynamic content)
        {
            //!!Content Type List!!
            return content switch
            {
                FileContent => "File",
                GeoJsonContent => "GeoJson",
                ImageContent => "Image",
                LineContent => "Line",
                LinkContent => "Link",
                NoteContent => "Note",
                PhotoContent => "Photo",
                PostContent => "Post",
                PointContent => "Point",
                PointContentDto => "Point",
                _ => string.Empty
            };
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
            var dbPath = UserSettingsSingleton.CurrentSettings().DatabaseFileFullName();
            return new PointlessWaymarksContext(optionsBuilder.UseSqlite($"Data Source={dbPath}").Options);
        }

        /// <summary>
        ///     Uses reflection to Trim and Convert Nulls to Empty on all string properties, truncate DateTimes to the
        ///     second and round Spatial values to an appropriately.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toProcess"></param>
        public static void DefaultPropertyCleanup<T>(T toProcess)
        {
            StringHelpers.TrimNullToEmptyAllStringProperties(toProcess);
            DateTimeHelpers.TrimDateTimesToSeconds(toProcess);
            SpatialHelpers.RoundSpatialValues(toProcess);
        }

        public static async Task<List<HistoricFileContent>> DeletedFileContent()
        {
            var db = await Context().ConfigureAwait(false);

            var deletedContent = await (from h in db.HistoricFileContents
                where !db.FileContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync().ConfigureAwait(false);

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricGeoJsonContent>> DeletedGeoJsonContent()
        {
            var db = await Context().ConfigureAwait(false);

            var deletedContent = await (from h in db.HistoricGeoJsonContents
                where !db.GeoJsonContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync().ConfigureAwait(false);

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricImageContent>> DeletedImageContent()
        {
            var db = await Context().ConfigureAwait(false);

            var deletedContent = await (from h in db.HistoricImageContents
                where !db.ImageContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync().ConfigureAwait(false);

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricLineContent>> DeletedLineContent()
        {
            var db = await Context().ConfigureAwait(false);

            var deletedContent = await (from h in db.HistoricLineContents
                where !db.LineContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync().ConfigureAwait(false);

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricLinkContent>> DeletedLinkContent()
        {
            var db = await Context().ConfigureAwait(false);

            var deletedContent = await (from h in db.HistoricLinkContents
                where !db.LinkContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync().ConfigureAwait(false);

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricNoteContent>> DeletedNoteContent()
        {
            var db = await Context().ConfigureAwait(false);

            var deletedContent = await (from h in db.HistoricNoteContents
                where !db.NoteContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync().ConfigureAwait(false);

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricPhotoContent>> DeletedPhotoContent()
        {
            var db = await Context().ConfigureAwait(false);

            var deletedContent = await (from h in db.HistoricPhotoContents
                where !db.PhotoContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync().ConfigureAwait(false);

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricPointContent>> DeletedPointContent()
        {
            var db = await Context().ConfigureAwait(false);

            var deletedContent = await (from h in db.HistoricPointContents
                where !db.PointContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync().ConfigureAwait(false);

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task<List<HistoricPostContent>> DeletedPostContent()
        {
            var db = await Context().ConfigureAwait(false);

            var deletedContent = await (from h in db.HistoricPostContents
                where !db.PostContents.Any(x => x.ContentId == h.ContentId)
                select h).ToListAsync().ConfigureAwait(false);

            return deletedContent.GroupBy(x => x.ContentId)
                .Select(x => x.OrderByDescending(y => y.ContentVersion).First()).ToList();
        }

        public static async Task DeleteFileContent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.FileContents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

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
                await context.HistoricFileContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.FileContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.File,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeleteGeoJsonContent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.GeoJsonContents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricGeoJsonContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricGeoJsonContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.GeoJsonContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.GeoJson,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeleteImageContent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.ImageContents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

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
                await context.HistoricImageContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.ImageContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Image,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeleteLineContent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.LineContents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricLineContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricLineContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.LineContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Line,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeleteLinkContent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.LinkContents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

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
                await context.HistoricLinkContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.LinkContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Link,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeleteMapComponent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var lastUpdatedOnForHistoric = DateTime.Now;

            var toHistoric = await context.MapComponents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

            if (!toHistoric.Any()) return;

            progress?.Report($"Writing {toHistoric.First().Title} Last Historic Entry");

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricMapComponent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = lastUpdatedOnForHistoric;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricMapComponents.AddAsync(newHistoric).ConfigureAwait(false);
                context.MapComponents.Remove(loopToHistoric);
            }

            var elementsToDelete =
                context.MapComponentElements.Where(x => x.MapComponentContentId == contentId).ToList();
            var elementsToDeleteContentIds = elementsToDelete.Select(x => x.ElementContentId).ToList();

            foreach (var loopElements in elementsToDelete)
            {
                await context.HistoricMapComponentElements.AddAsync(new HistoricMapElement
                {
                    ElementContentId = loopElements.ElementContentId,
                    ShowDetailsDefault = loopElements.ShowDetailsDefault,
                    IncludeInDefaultView = loopElements.IncludeInDefaultView,
                    LastUpdateOn = lastUpdatedOnForHistoric,
                    MapComponentContentId = loopElements.MapComponentContentId
                }).ConfigureAwait(false);

                context.MapComponentElements.Remove(loopElements);
            }

            await context.SaveChangesAsync().ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Map,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());

            if (elementsToDeleteContentIds.Any())
                DataNotifications.PublishDataNotification("Db", DataNotificationContentType.MapElement,
                    DataNotificationUpdateType.Delete, elementsToDeleteContentIds);
        }

        public static async Task DeleteNoteContent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.NoteContents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

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
                await context.HistoricNoteContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.NoteContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Note,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeletePhotoContent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.PhotoContents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

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
                await context.HistoricPhotoContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.PhotoContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Photo,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeletePointContent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var groupLastUpdateOn = DateTime.Now;
            var updateGroup = Guid.NewGuid();

            var toHistoric = await context.PointContents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

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
                await context.HistoricPointContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.PointContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            var relatedDetails = context.PointDetails.Where(x => x.PointContentId == contentId).ToList();

            foreach (var loopToHistoric in relatedDetails)
            {
                var newHistoric = new HistoricPointDetail();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = groupLastUpdateOn;
                newHistoric.HistoricGroupId = updateGroup;
                await context.HistoricPointDetails.AddAsync(newHistoric).ConfigureAwait(false);
            }

            context.PointDetails.RemoveRange(relatedDetails);

            await context.SaveChangesAsync().ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Point,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
                DataNotificationUpdateType.Delete, relatedDetails.Select(x => x.ContentId).ToList());
        }

        public static async Task DeletePostContent(Guid contentId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.PostContents.Where(x => x.ContentId == contentId).ToListAsync().ConfigureAwait(false);

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
                await context.HistoricPostContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.PostContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"{toHistoric.First().Title} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Post,
                DataNotificationUpdateType.Delete, toHistoric.Select(x => x.ContentId).ToList());
        }

        public static async Task DeleteTagExclusion(int tagExclusionDbEntryId, IProgress<string>? progress = null)
        {
            var context = await Context().ConfigureAwait(false);

            var toDelete = context.TagExclusions.Single(x => x.Id == tagExclusionDbEntryId);

            context.TagExclusions.Remove(toDelete);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            progress?.Report($"Tag Exclusion {toDelete.Tag} Deleted");

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.TagExclusion,
                DataNotificationUpdateType.Delete, null);
        }

        public static async Task<List<string>> FolderNamesFromContent(dynamic content)
        {
            var db = await Context().ConfigureAwait(false);

            return content switch
            {
                FileContent => db.FileContents.Where(x => !string.IsNullOrWhiteSpace(x.Folder)).Select(x => x.Folder)
                    .Distinct().OrderBy(x => x).Cast<string>().ToList(),
                GeoJsonContent => db.GeoJsonContents.Where(x => !string.IsNullOrWhiteSpace(x.Folder))
                    .Select(x => x.Folder).Distinct().OrderBy(x => x).Cast<string>().ToList(),
                ImageContent => db.ImageContents.Where(x => !string.IsNullOrWhiteSpace(x.Folder)).Select(x => x.Folder)
                    .Distinct().OrderBy(x => x).Cast<string>().ToList(),
                LineContent => db.LineContents.Where(x => !string.IsNullOrWhiteSpace(x.Folder)).Select(x => x.Folder)
                    .Distinct().OrderBy(x => x).Cast<string>().ToList(),
                NoteContent => db.NoteContents.Where(x => !string.IsNullOrWhiteSpace(x.Folder)).Select(x => x.Folder)
                    .Distinct().OrderBy(x => x).Cast<string>().ToList(),
                PhotoContent => db.PhotoContents.Where(x => !string.IsNullOrWhiteSpace(x.Folder)).Select(x => x.Folder)
                    .Distinct().OrderBy(x => x).Cast<string>().ToList(),
                PointContent => db.PointContents.Where(x => !string.IsNullOrWhiteSpace(x.Folder)).Select(x => x.Folder)
                    .Distinct().OrderBy(x => x).Cast<string>().ToList(),
                PointContentDto => db.PointContents.Where(x => !string.IsNullOrWhiteSpace(x.Folder))
                    .Select(x => x.Folder).Distinct().OrderBy(x => x).Cast<string>().ToList(),
                PostContent => db.PostContents.Where(x => !string.IsNullOrWhiteSpace(x.Folder)).Select(x => x.Folder)
                    .Distinct().OrderBy(x => x).Cast<string>().ToList(),
                _ => new List<string>()
            };
        }

        public static async Task<List<HistoricPointDetail>> HistoricPointDetailsForPoint(Guid pointContentId,
            PointlessWaymarksContext db, int entriesToReturn)
        {
            return await db.HistoricPointDetails.Where(x => x.PointContentId == pointContentId).Take(entriesToReturn)
                .ToListAsync().ConfigureAwait(false);
        }

        public static async Task<List<IContentCommon>> MainFeedCommonContent()
        {
            var db = await Context().ConfigureAwait(false);
            var fileContent =
                await db.FileContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var geoJsonContent = await db.GeoJsonContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft).Cast<IContentCommon>()
                .ToListAsync().ConfigureAwait(false);
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft).Cast<IContentCommon>()
                .ToListAsync().ConfigureAwait(false);
            var lineContent =
                await db.LineContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var noteContent =
                await db.NoteContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft).Cast<IContentCommon>()
                .ToListAsync().ConfigureAwait(false);
            var pointContent = await db.PointContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft).Cast<IContentCommon>()
                .ToListAsync().ConfigureAwait(false);
            var postContent =
                await db.PostContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);

            return fileContent.Concat(geoJsonContent).Concat(imageContent).Concat(lineContent).Concat(noteContent)
                .Concat(postContent).Concat(photoContent).Concat(pointContent).OrderByDescending(x => x.FeedOn)
                .ToList();
        }

        public static async Task<List<IContentCommon>> MainFeedCommonContentAfter(DateTime after, int numberOfEntries)
        {
            var db = await Context().ConfigureAwait(false);
            var fileContent = await db.FileContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && x.FeedOn > after)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var geoJsonContent = await db.GeoJsonContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && x.FeedOn > after)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && x.FeedOn > after)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var lineContent = await db.LineContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && x.FeedOn > after)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && x.FeedOn > after)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && x.FeedOn > after)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var pointContent = await db.PointContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && x.FeedOn > after)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var postContent = await db.PostContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && x.FeedOn > after)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);

            return fileContent.Concat(geoJsonContent).Concat(imageContent).Concat(lineContent).Concat(noteContent)
                .Concat(photoContent).Concat(postContent).Concat(pointContent).OrderBy(x => x.FeedOn)
                .Take(numberOfEntries).ToList();
        }

        public static async Task<List<IContentCommon>> MainFeedCommonContentBefore(DateTime before, int numberOfEntries)
        {
            var db = await Context().ConfigureAwait(false);
            var fileContent = await db.FileContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft && x.FeedOn < before)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var geoJsonContent = await db.GeoJsonContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft && x.FeedOn < before)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft && x.FeedOn < before)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var lineContent = await db.LineContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft && x.FeedOn < before)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft && x.FeedOn < before)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft && x.FeedOn < before)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var pointContent = await db.PointContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft && x.FeedOn < before)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);
            var postContent = await db.PostContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft && x.FeedOn < before)
                .OrderByDescending(x => x.FeedOn).Take(numberOfEntries).Cast<IContentCommon>().ToListAsync().ConfigureAwait(false);

            return fileContent.Concat(geoJsonContent).Concat(imageContent).Concat(lineContent).Concat(noteContent)
                .Concat(postContent).Concat(photoContent).Concat(pointContent).OrderByDescending(x => x.FeedOn)
                .Take(numberOfEntries).ToList();
        }

        public static async Task<List<dynamic>> MainFeedRecentDynamicContent(int topNumberOfEntries)
        {
            var db = await Context().ConfigureAwait(false);
            var fileContent = await db.FileContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft).OrderByDescending(x => x.FeedOn)
                .Take(topNumberOfEntries).Cast<dynamic>().ToListAsync().ConfigureAwait(false);
            var geoJsonContent = await db.GeoJsonContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft)
                .OrderByDescending(x => x.FeedOn).Take(topNumberOfEntries).Cast<dynamic>().ToListAsync().ConfigureAwait(false);
            var imageContent = await db.ImageContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft)
                .OrderByDescending(x => x.FeedOn).Take(topNumberOfEntries).Cast<dynamic>().ToListAsync().ConfigureAwait(false);
            var lineContent = await db.LineContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft).OrderByDescending(x => x.FeedOn)
                .Take(topNumberOfEntries).Cast<dynamic>().ToListAsync().ConfigureAwait(false);
            var noteContent = await db.NoteContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft).OrderByDescending(x => x.FeedOn)
                .Take(topNumberOfEntries).Cast<dynamic>().ToListAsync().ConfigureAwait(false);
            var photoContent = await db.PhotoContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft)
                .OrderByDescending(x => x.FeedOn).Take(topNumberOfEntries).Cast<dynamic>().ToListAsync().ConfigureAwait(false);
            var pointContent = (await db.PointContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft)
                    .OrderByDescending(x => x.FeedOn).Take(topNumberOfEntries).Select(x => x.ContentId)
                    .ToListAsync().ConfigureAwait(false))
                .Select(x => PointAndPointDetails(x).Result).Cast<dynamic>().ToList();
            var postContent = await db.PostContents.Where(x => x.ShowInMainSiteFeed && !x.IsDraft && !x.IsDraft).OrderByDescending(x => x.FeedOn)
                .Take(topNumberOfEntries).Cast<dynamic>().ToListAsync().ConfigureAwait(false);


            return fileContent.Concat(geoJsonContent).Concat(imageContent).Concat(lineContent).Concat(noteContent)
                .Concat(photoContent).Concat(pointContent).Concat(postContent).OrderByDescending(x => x.FeedOn)
                .Take(topNumberOfEntries).ToList();
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

        public static async Task<MapComponentDto> MapComponentDtoFromContentId(Guid mapComponentGuid)
        {
            var db = await Context().ConfigureAwait(false);

            var map = db.MapComponents.Single(x => x.ContentId == mapComponentGuid);
            var elements = await db.MapComponentElements.Where(x => x.MapComponentContentId == mapComponentGuid)
                .ToListAsync().ConfigureAwait(false);

            return new MapComponentDto(map, elements);
        }

        public static List<(string tag, List<object> contentObjects)> ParseToTagSlugsAndContentList(List<ITag>? toAdd,
            bool removeExcludedTags, IProgress<string>? progress = null)
        {
            var returnList = new List<(string tag, List<object> contentObjects)>();

            if (toAdd == null) return returnList;

            var allResults = new ConcurrentBag<(string tag, object contentObject)>();

            Parallel.ForEach(toAdd, x =>
            {
                var results = ParseToTagSlugsAndContentList(x, removeExcludedTags);

                results.ForEach(y => allResults.Add(y));
            });

            var projectedReturn = allResults.GroupBy(x => x.tag)
                .Select(x => (x.Key, x.Select(y => y.contentObject).ToList())).ToList();

            return projectedReturn;
        }

        public static List<(string tag, object contentObject)> ParseToTagSlugsAndContentList(ITag toAdd,
            bool removeExcludedTags)
        {
            var tags = TagListParseToSlugs(toAdd, removeExcludedTags);

            return !tags.Any()
                ? new List<(string tag, object contentObject)>()
                : tags.Select(loopTags => (loopTags, (object) toAdd)).ToList();
        }

        public static async Task<PointContentDto?> PointAndPointDetails(Guid pointContentId)
        {
            var db = await Context().ConfigureAwait(false);

            return await PointAndPointDetails(pointContentId, db).ConfigureAwait(false);
        }

        public static async Task<PointContentDto?> PointAndPointDetails(Guid pointContentId,
            PointlessWaymarksContext db)
        {
            var point = await db.PointContents.SingleAsync(x => x.ContentId == pointContentId).ConfigureAwait(false);
            var details = await db.PointDetails.Where(x => x.PointContentId == pointContentId).ToListAsync().ConfigureAwait(false);

            var toReturn = new PointContentDto();
            toReturn.InjectFrom(point);
            toReturn.PointDetails = details;

            return toReturn;
        }

        public static async Task<List<PointContentDto>> PointAndPointDetails(List<Guid>? pointContentIdList,
            PointlessWaymarksContext db)
        {
            if (pointContentIdList == null) return new List<PointContentDto>();

            var returnList = new List<PointContentDto>();

            foreach (var loopId in pointContentIdList)
            {
                var toAdd = await PointAndPointDetails(loopId, db).ConfigureAwait(false);
                if (toAdd != null) returnList.Add(toAdd);
            }

            return returnList;
        }

        public static PointContentDto PointContentDtoFromPointContentAndDetails(PointContent content,
            List<PointDetail> details)
        {
            var toReturn = new PointContentDto();

            toReturn.InjectFrom(content);

            toReturn.PointDetails = details;

            return toReturn;
        }

        public static (PointContent content, List<PointDetail> details) PointContentDtoToPointContentAndDetails(
            PointContentDto dto)
        {
            var toSave = (PointContent) new PointContent().InjectFrom(dto);
            var relatedDetails = dto.PointDetails;

            return (toSave, relatedDetails);
        }

        public static IPointDetailData? PointDetailDataFromIdentifierAndJson(string dataIdentifier, string json)
        {
            return dataIdentifier switch
            {
                "Campground" => JsonSerializer.Deserialize<Campground>(json),
                "Feature" => JsonSerializer.Deserialize<Feature>(json),
                "Fee" => JsonSerializer.Deserialize<Fee>(json),
                "Driving Directions" => JsonSerializer.Deserialize<DrivingDirections>(json),
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
                var typeExample = (IPointDetailData?) Activator.CreateInstance(loopTypes);

                if (typeExample == null) continue;

                if (typeExample.DataTypeIdentifier == dataType) return true;
            }

            return false;
        }

        public static async Task<List<PointDetail>> PointDetailsForPoint(Guid pointContentId,
            PointlessWaymarksContext db)
        {
            var details = await db.PointDetails.Where(x => x.PointContentId == pointContentId).ToListAsync().ConfigureAwait(false);

            return details;
        }

        public static async Task<List<PointContentDto>> PointsAndPointDetails(List<Guid> pointContentId)
        {
            var db = await Context().ConfigureAwait(false);

            var idChunks = pointContentId.Chunk(250);

            var returnList = new List<PointContentDto>();

            foreach (var loopChunk in idChunks)
            {
                var contents = await db.PointContents.Where(x => loopChunk.Contains(x.ContentId)).ToListAsync().ConfigureAwait(false);

                foreach (var loopContent in contents)
                {
                    var details = await PointDetailsForPoint(loopContent.ContentId, db).ConfigureAwait(false);
                    var toAdd = new PointContentDto();
                    toAdd.InjectFrom(loopContent);
                    toAdd.PointDetails = details;

                    returnList.Add(toAdd);
                }
            }

            return returnList;
        }

        public static async Task SaveFileContent(FileContent? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.FileContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricFileContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricFileContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.FileContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            await context.FileContents.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.File,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SaveGenerationFileTransferScriptLog(GenerationFileTransferScriptLog? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            await context.GenerationFileTransferScriptLogs.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync().ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.FileTransferScriptLog,
                DataNotificationUpdateType.New, new List<Guid>());
        }

        public static async Task SaveGenerationLogAndRecordSettings(DateTime generationVersion)
        {
            var db = await Context().ConfigureAwait(false);

            var serializedSettings =
                JsonSerializer.Serialize(UserSettingsSingleton.CurrentSettings().GenerationValues());
            var dbGenerationRecord = new GenerationLog
            {
                GenerationSettings = serializedSettings, GenerationVersion = generationVersion
            };

            await db.GenerationLogs.AddAsync(dbGenerationRecord).ConfigureAwait(false);
            await db.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.GenerationLog,
                DataNotificationUpdateType.New, new List<Guid>());
        }

        public static async Task SaveGeoJsonContent(GeoJsonContent? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.GeoJsonContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricGeoJsonContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricGeoJsonContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.GeoJsonContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync().ConfigureAwait(false);

            if (toSave.Id > 0) toSave.Id = 0;

            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            var boundingBox = SpatialConverters.GeometryBoundingBox(toSave);

            toSave.InitialViewBoundsMaxLatitude = boundingBox.MaxY;
            toSave.InitialViewBoundsMaxLongitude = boundingBox.MaxX;
            toSave.InitialViewBoundsMinLatitude = boundingBox.MinY;
            toSave.InitialViewBoundsMinLongitude = boundingBox.MinX;
            DefaultPropertyCleanup(toSave);

            await context.GeoJsonContents.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.GeoJson,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SaveImageContent(ImageContent? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.ImageContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricImageContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricImageContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.ImageContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.ImageContents.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            toSave.MainPicture = toSave.ContentId;

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Image,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SaveLineContent(LineContent? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.LineContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricLineContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricLineContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.LineContents.Remove(loopToHistoric);
            }

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            if (toSave.Id > 0) toSave.Id = 0;

            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            var boundingBox = SpatialConverters.GeometryBoundingBox(toSave);

            toSave.InitialViewBoundsMaxLatitude = boundingBox.MaxY;
            toSave.InitialViewBoundsMaxLongitude = boundingBox.MaxX;
            toSave.InitialViewBoundsMinLatitude = boundingBox.MinY;
            toSave.InitialViewBoundsMinLongitude = boundingBox.MinX;
            DefaultPropertyCleanup(toSave);

            await context.LineContents.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Line,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SaveLinkContent(LinkContent? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.LinkContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricLinkContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricLinkContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.LinkContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.LinkContents.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Link,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task<MapComponentDto> SaveMapComponent(MapComponentDto toSaveDto)
        {
            var context = await Context().ConfigureAwait(false);

            var groupLastUpdateOn = DateTime.Now;
            var updateGroup = Guid.NewGuid();

            var toHistoric = await context.MapComponents.Where(x => x.ContentId == toSaveDto.Map.ContentId)
                .ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricMapComponent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = groupLastUpdateOn;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricMapComponents.AddAsync(newHistoric).ConfigureAwait(false);
                context.MapComponents.Remove(loopToHistoric);
            }

            if (toSaveDto.Map.Id > 0) toSaveDto.Map.Id = 0;
            toSaveDto.Map.ContentVersion = ContentVersionDateTime();

            await context.MapComponents.AddAsync(toSaveDto.Map).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            var dbElements = await context.MapComponentElements
                .Where(x => x.MapComponentContentId == toSaveDto.Map.ContentId).ToListAsync().ConfigureAwait(false);

            var dbElementContentIds = dbElements.Select(x => x.ElementContentId).Distinct().ToList();

            foreach (var loopElements in dbElements)
            {
                await context.HistoricMapComponentElements.AddAsync(new HistoricMapElement
                {
                    ElementContentId = loopElements.ElementContentId,
                    ShowDetailsDefault = loopElements.ShowDetailsDefault,
                    IncludeInDefaultView = loopElements.IncludeInDefaultView,
                    IsFeaturedElement = loopElements.IsFeaturedElement,
                    LastUpdateOn = groupLastUpdateOn,
                    HistoricGroupId = updateGroup,
                    MapComponentContentId = loopElements.MapComponentContentId
                }).ConfigureAwait(false);

                context.MapComponentElements.Remove(loopElements);
            }

            await context.SaveChangesAsync().ConfigureAwait(false);

            var newElementsContentIds = toSaveDto.Elements.Select(x => x.ElementContentId).ToList();

            foreach (var loopElements in toSaveDto.Elements)
            {
                loopElements.Id = 0;
                await context.MapComponentElements.AddAsync(loopElements).ConfigureAwait(false);
            }

            await context.SaveChangesAsync().ConfigureAwait(false);

            var points = await context.PointContents.Where(x => newElementsContentIds.Contains(x.ContentId))
                .ToListAsync().ConfigureAwait(false);
            var boundingBox = SpatialConverters.PointBoundingBox(points);

            var geoJsonLines = await context.LineContents.Where(x => newElementsContentIds.Contains(x.ContentId))
                .ToListAsync().ConfigureAwait(false);
            boundingBox = SpatialConverters.GeometryBoundingBox(geoJsonLines, boundingBox);

            var geoJson = await context.GeoJsonContents.Where(x => newElementsContentIds.Contains(x.ContentId))
                .ToListAsync().ConfigureAwait(false);
            boundingBox = SpatialConverters.GeometryBoundingBox(geoJson, boundingBox);

            toSaveDto.Map.InitialViewBoundsMaxLatitude = boundingBox.MaxY;
            toSaveDto.Map.InitialViewBoundsMaxLongitude = boundingBox.MaxX;
            toSaveDto.Map.InitialViewBoundsMinLatitude = boundingBox.MinY;
            toSaveDto.Map.InitialViewBoundsMinLongitude = boundingBox.MinX;
            DefaultPropertyCleanup(toSaveDto.Map);

            await context.SaveChangesAsync().ConfigureAwait(false);

            var newElements = newElementsContentIds.Except(dbElementContentIds).ToList();
            var updatedElements = newElementsContentIds.Where(x => dbElementContentIds.Contains(x)).ToList();
            var deletedElements = dbElementContentIds.Except(newElementsContentIds).ToList();

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Map,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSaveDto.Map.ContentId});

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.MapElement,
                DataNotificationUpdateType.New, newElements);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.MapElement,
                DataNotificationUpdateType.Update, updatedElements);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.MapElement,
                DataNotificationUpdateType.Delete, deletedElements);

            return toSaveDto;
        }

        public static async Task SaveNoteContent(NoteContent? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.NoteContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricNoteContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricNoteContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.NoteContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.NoteContents.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Note,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SavePhotoContent(PhotoContent? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.PhotoContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPhotoContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricPhotoContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.PhotoContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            await context.PhotoContents.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            toSave.MainPicture = toSave.ContentId;

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Photo,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task<PointContentDto?> SavePointContent(PointContentDto? toSaveDto)
        {
            if (toSaveDto == null) return null;

            var (toSave, relatedDetails) = PointContentDtoToPointContentAndDetails(toSaveDto);

            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.PointContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPointContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricPointContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.PointContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            await context.PointContents.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            await SavePointDetailContent(relatedDetails).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Point,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                toSave.ContentId.AsList());

            return await PointAndPointDetails(toSaveDto.ContentId).ConfigureAwait(false);
        }

        public static async Task SavePointDetailContent(List<PointDetail> toSave)
        {
            if (!toSave.Any()) return;

            if (toSave.Select(x => x.PointContentId).Distinct().Count() > 1)
            {
                var grouped = toSave.GroupBy(x => x.PointContentId).ToList();

                foreach (var loopGroups in grouped) await SavePointDetailContent(loopGroups.Select(x => x).ToList()).ConfigureAwait(false);

                return;
            }

            //The code above is intended to insure that by this point all the PointDetails to save are related to the same PointContent

            var context = await Context().ConfigureAwait(false);

            var groupLastUpdateOn = DateTime.Now;
            var updateGroup = Guid.NewGuid();

            var toSaveGuids = toSave.Select(x => x.ContentId).ToList();
            var relatedContentGuid = toSave.First().PointContentId;

            var currentEntriesFromPoint =
                await context.PointDetails.Where(x => x.PointContentId == relatedContentGuid).ToListAsync().ConfigureAwait(false);
            var detailsToReplace = currentEntriesFromPoint.Where(x => toSaveGuids.Contains(x.ContentId)).ToList();

            //The logic here is that if there are items to remove it is an update and if not the item is new
            var updatedDetailIds = toSave.Where(x => detailsToReplace.Select(y => y.ContentId).Contains(x.ContentId))
                .Select(x => x.ContentId).ToList();
            var newDetailIds = toSave.Where(x => !detailsToReplace.Select(y => y.ContentId).Contains(x.ContentId))
                .Select(x => x.ContentId).ToList();

            foreach (var loopToHistoric in currentEntriesFromPoint)
            {
                var newHistoric = new HistoricPointDetail();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = groupLastUpdateOn;
                newHistoric.HistoricGroupId = updateGroup;
                await context.HistoricPointDetails.AddAsync(newHistoric).ConfigureAwait(false);
            }

            var deletedContentIds = currentEntriesFromPoint.Select(x => x.ContentId).Except(updatedDetailIds)
                .Except(newDetailIds).ToList();

            context.PointDetails.RemoveRange(currentEntriesFromPoint);

            await context.SaveChangesAsync().ConfigureAwait(false);

            toSave.ForEach(x => x.ContentVersion = ContentVersionDateTime());

            await context.PointDetails.AddRangeAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            if (deletedContentIds.Any())
                DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
                    DataNotificationUpdateType.Delete, updatedDetailIds);

            if (updatedDetailIds.Any())
                DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
                    DataNotificationUpdateType.Update, updatedDetailIds);

            if (newDetailIds.Any())
                DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
                    DataNotificationUpdateType.New, updatedDetailIds);
        }

        /// <summary>
        ///     Save a PointDetail - saving a single point detail will save the entire current set of Point Details
        ///     into Historic Details to preserve history - it is more efficient to submit all changes at once.
        /// </summary>
        /// <param name="toSave"></param>
        /// <returns></returns>
        public static async Task SavePointDetailContent(PointDetail? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            var groupLastUpdateOn = DateTime.Now;
            var updateGroup = Guid.NewGuid();

            var currentEntriesFromPoint =
                await context.PointDetails.Where(x => x.PointContentId == toSave.PointContentId).ToListAsync().ConfigureAwait(false);
            var detailsToReplace = currentEntriesFromPoint.Where(x => x.ContentId == toSave.ContentId).ToList();
            var isUpdate = detailsToReplace.Any();

            foreach (var loopToHistoric in currentEntriesFromPoint)
            {
                var newHistoric = new HistoricPointDetail();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = groupLastUpdateOn;
                newHistoric.HistoricGroupId = updateGroup;
                await context.HistoricPointDetails.AddAsync(newHistoric).ConfigureAwait(false);
            }

            context.PointDetails.RemoveRange(detailsToReplace);

            await context.SaveChangesAsync().ConfigureAwait(false);

            if (toSave.Id > 0) toSave.Id = 0;

            toSave.ContentVersion = ContentVersionDateTime();

            await context.PointDetails.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.PointDetail,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task SavePostContent(PostContent? toSave)
        {
            if (toSave == null) return;

            var context = await Context().ConfigureAwait(false);

            var toHistoric = await context.PostContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync().ConfigureAwait(false);

            var isUpdate = toHistoric.Any();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPostContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                newHistoric.LastUpdatedOn = DateTime.Now;
                if (string.IsNullOrWhiteSpace(newHistoric.LastUpdatedBy))
                    newHistoric.LastUpdatedBy = "Historic Entry Archivist";
                await context.HistoricPostContents.AddAsync(newHistoric).ConfigureAwait(false);
                context.PostContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;
            toSave.ContentVersion = ContentVersionDateTime();

            toSave.MainPicture = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(toSave.BodyContent);

            await context.PostContents.AddAsync(toSave).ConfigureAwait(false);

            await context.SaveChangesAsync(true).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Db", DataNotificationContentType.Post,
                isUpdate ? DataNotificationUpdateType.Update : DataNotificationUpdateType.New,
                new List<Guid> {toSave.ContentId});
        }

        public static async Task<List<TagExclusion>> TagExclusions()
        {
            var context = await Context().ConfigureAwait(false);

            return await context.TagExclusions.OrderBy(x => x.Tag).ToListAsync().ConfigureAwait(false);
        }

        public static async Task<List<(string slug, TagExclusion exclusion)>> TagExclusionSlugAndExclusions()
        {
            var context = await Context().ConfigureAwait(false);

            return (await context.TagExclusions.OrderBy(x => x.Tag).ToListAsync().ConfigureAwait(false))
                .Select(x => (SlugUtility.Create(true, x.Tag, 200), x)).ToList();
        }

        public static async Task<List<string>> TagExclusionSlugs()
        {
            var context = await Context().ConfigureAwait(false);

            return (await context.TagExclusions.OrderBy(x => x.Tag).ToListAsync().ConfigureAwait(false))
                .Select(x => SlugUtility.Create(true, x.Tag, 200)).ToList();
        }

        public static string TagListCleanup(string? tags)
        {
            return string.IsNullOrWhiteSpace(tags) ? string.Empty : TagListJoin(TagListParse(tags));
        }

        public static List<string> TagListCleanup(List<string> listToClean)
        {
            if (!listToClean.Any()) return new List<string>();

            return listToClean.Select(TagListItemCleanup).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        }

        /// <summary>
        ///     Use to clean up a single tag - trims and removes inner multi-space
        /// </summary>
        /// <param name="toClean"></param>
        /// <returns></returns>
        public static string TagListItemCleanup(string? toClean)
        {
            if (string.IsNullOrWhiteSpace(toClean)) return string.Empty;

            return Regex.Replace(SlugUtility.CreateSpacedString(true, toClean, 200), @"\s+", " ").TrimNullToEmpty()
                .ToLower();
        }

        public static string TagListJoin(List<string> tagList)
        {
            if (tagList.Count < 1) return string.Empty;

            var cleanedList = tagList.Select(TagListItemCleanup).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()
                .OrderBy(x => x).ToList();

            return string.Join(",", cleanedList);
        }

        public static string TagListJoinAsSlugs(List<string> tagList, bool removeExcludedTags)
        {
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

        public static List<string> TagListParse(string? rawTagString)
        {
            if (rawTagString == null) return new List<string>();
            if (string.IsNullOrWhiteSpace(rawTagString)) return new List<string>();

            return rawTagString.Split(",").Select(TagListItemCleanup).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct().OrderBy(x => x).ToList();
        }

        public static List<string> TagListParseToSlugs(string? rawTagString, bool removeExcludedTags)
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

        public static List<string> TagListParseToSlugs(ITag? tag, bool removeExcludedTags)
        {
            if (tag == null) return new List<string>();
            if (string.IsNullOrWhiteSpace(tag.Tags)) return new List<string>();

            return TagListParseToSlugs(tag.Tags, removeExcludedTags);
        }

        public static List<TagSlugAndIsExcluded> TagListParseToSlugsAndIsExcluded(ITag? tag)
        {
            if (tag == null) return new List<TagSlugAndIsExcluded>();
            if (string.IsNullOrWhiteSpace(tag.Tags)) return new List<TagSlugAndIsExcluded>();

            return TagListParseToSlugsAndIsExcluded(tag.Tags);
        }

        public static List<TagSlugAndIsExcluded> TagListParseToSlugsAndIsExcluded(string? rawTagString)
        {
            if (rawTagString == null) return new List<TagSlugAndIsExcluded>();
            if (string.IsNullOrWhiteSpace(rawTagString)) return new List<TagSlugAndIsExcluded>();

            var db = Context().Result;
            var excludedTags = db.TagExclusions.ToList().Select(x => SlugUtility.Create(true, x.Tag, 200)).ToList();

            var tagSlugs = rawTagString.Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
                .Select(x => SlugUtility.Create(true, x, 200)).Distinct().ToList();

            return tagSlugs.Select(x => new TagSlugAndIsExcluded(x, excludedTags.Contains(x))).ToList();
        }

        public static async Task<List<(string tag, List<dynamic> contentObjects)>> TagSlugsAndContentList(
            bool includePagesExcludedFromSearch, bool removeExcludedTags, IProgress<string>? progress = null)
        {
            progress?.Report("Starting Parse of Tag Content");

            var tagBag = new List<List<(string tag, List<dynamic> contentObjects)>>();

            await new List<Func<Task>>
            {
                async () =>
                {
                    progress?.Report("Process File Content Tags");
                    var db = await Context().ConfigureAwait(false);
                    tagBag.Add(ParseToTagSlugsAndContentList(
                        (await db.FileContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList(),
                        removeExcludedTags, progress));
                },
                async () =>
                {
                    progress?.Report("Process GeoJson Content Tags");
                    var db = await Context().ConfigureAwait(false);
                    tagBag.Add(ParseToTagSlugsAndContentList(
                        (await db.GeoJsonContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList(),
                        removeExcludedTags, progress));
                },
                async () =>
                {
                    progress?.Report("Process Image Content Tags");
                    var db = await Context().ConfigureAwait(false);
                    tagBag.Add(ParseToTagSlugsAndContentList(
                        includePagesExcludedFromSearch
                            ? (await db.ImageContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList()
                            : (await db.ImageContents.Where(y => !y.IsDraft && y.ShowInSearch).ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList(),
                        removeExcludedTags, progress));
                },
                async () =>
                {
                    progress?.Report("Process Line Content Tags");
                    var db = await Context().ConfigureAwait(false);
                    tagBag.Add(ParseToTagSlugsAndContentList(
                        (await db.LineContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList(),
                        removeExcludedTags, progress));
                },
                async () =>
                {
                    progress?.Report("Process Link Content Tags");
                    var db = await Context().ConfigureAwait(false);
                    tagBag.Add(ParseToTagSlugsAndContentList(
                        (await db.LinkContents.ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList(),
                        removeExcludedTags, progress));
                },
                async () =>
                {
                    progress?.Report("Process Note Content Tags");
                    var db = await Context().ConfigureAwait(false);
                    tagBag.Add(ParseToTagSlugsAndContentList(
                        (await db.NoteContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList(),
                        removeExcludedTags, progress));
                },
                async () =>
                {
                    progress?.Report("Process Photo Content Tags");
                    var db = await Context().ConfigureAwait(false);
                    tagBag.Add(ParseToTagSlugsAndContentList(
                        (await db.PhotoContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList(),
                        removeExcludedTags, progress));
                },
                async () =>
                {
                    progress?.Report("Process Point Content Tags");
                    var db = await Context().ConfigureAwait(false);
                    tagBag.Add(ParseToTagSlugsAndContentList(
                        (await db.PointContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList(),
                        removeExcludedTags, progress));
                },
                async () =>
                {
                    progress?.Report("Process Post Content Tags");
                    var db = await Context().ConfigureAwait(false);
                    tagBag.Add(ParseToTagSlugsAndContentList(
                        (await db.PostContents.Where(x => !x.IsDraft).ToListAsync().ConfigureAwait(false)).Cast<ITag>().ToList(),
                        removeExcludedTags, progress));
                }
            }.AsyncParallelForEach().ConfigureAwait(false);

            var flattened = tagBag.Where(x => x.Count > 0).SelectMany(x => x).ToList();

            var grouped = flattened.GroupBy(x => x.tag)
                .Select(x => (x.Key, x.SelectMany(y => y.contentObjects).ToList())).OrderBy(x => x.Key)
                .ToList();

            progress?.Report("Finished Parsing Tag Content");

            return grouped;
        }

        public record TagSlugAndIsExcluded(string TagSlug, bool IsExcluded);
    }
}