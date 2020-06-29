using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData
{
    public static class Db
    {
        public static async Task<PointlessWaymarksContext> Context()
        {
            var optionsBuilder = new DbContextOptionsBuilder<PointlessWaymarksContext>();
            var dbPath = UserSettingsSingleton.CurrentSettings().DatabaseFile;
            return new PointlessWaymarksContext(optionsBuilder.UseSqlite($"Data Source={dbPath}").Options);
        }

        public static async Task<EventLogContext> Log()
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

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricFileContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricFileContents.AddAsync(newHistoric);
                context.FileContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;

            await context.FileContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);
        }

        public static async Task SaveImageContent(ImageContent toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.ImageContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricImageContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricImageContents.AddAsync(newHistoric);
                context.ImageContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;

            await context.ImageContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);
        }

        public static async Task SaveLinkStream(LinkStream toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.LinkStreams.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricLinkStream();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricLinkStreams.AddAsync(newHistoric);
                context.LinkStreams.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;

            await context.LinkStreams.AddAsync(toSave);

            await context.SaveChangesAsync(true);
        }

        public static async Task SaveNoteContent(NoteContent toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.NoteContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricNoteContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricNoteContents.AddAsync(newHistoric);
                context.NoteContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;

            await context.NoteContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);
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

            await context.PhotoContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);

            if (isUpdate)
                DataNotifications.PhotoContentDataNotificationEventSource.Raise("Db Class",
                    new DataNotificationEventArgs
                    {
                        UpdateType = DataNotificationUpdateType.Update,
                        ContentIds = new List<Guid> {toSave.ContentId}
                    });

            DataNotifications.PhotoContentDataNotificationEventSource.Raise("Db Class",
                new DataNotificationEventArgs
                {
                    UpdateType = DataNotificationUpdateType.New, ContentIds = new List<Guid> {toSave.ContentId}
                });
        }

        public static async Task SavePostContent(PostContent toSave)
        {
            if (toSave == null) return;

            var context = await Context();

            var toHistoric = await context.PostContents.Where(x => x.ContentId == toSave.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPostContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricPostContents.AddAsync(newHistoric);
                context.PostContents.Remove(loopToHistoric);
            }

            if (toSave.Id > 0) toSave.Id = 0;

            await context.PostContents.AddAsync(toSave);

            await context.SaveChangesAsync(true);
        }

        public static async Task<List<(string tag, List<object> contentObjects)>> TagAndContentList(
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
            ParseToTagAndContentList(returnList, (await db.LinkStreams.ToListAsync()).Cast<ITag>().ToList(), progress);

            progress?.Report("Finished Parsing Tag Content");

            return returnList;
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

            return Regex.Replace(toClean, @"\s+", " ").TrimNullSafe().ToLower();
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
                excludedTags = db.TagExclusions.ToList().Select(x => SlugUtility.Create(true, x.Tag)).ToList();
            }

            var cleanedList = tagList.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => SlugUtility.Create(true, x.Trim())).Distinct().Except(excludedTags).OrderBy(x => x)
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
                excludedTags = db.TagExclusions.ToList().Select(x => SlugUtility.Create(true, x.Tag)).ToList();
            }

            return rawTagString.Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
                .Select(x => SlugUtility.Create(true, x)).Distinct().Where(x => !excludedTags.Contains(x))
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