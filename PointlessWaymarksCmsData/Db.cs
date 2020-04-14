using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            var dbPath = Path.Combine(UserSettingsSingleton.CurrentSettings().LocalMasterMediaArchive,
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
    }
}