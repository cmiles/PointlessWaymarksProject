using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;

namespace PointlessWaymarksCmsData
{
    public static class UserSettingsUtilities
    {
        public static string SettingsFileName = "PointlessWaymarksCmsSettings.json";

        public static string AllContentListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/AllContentList.html";
        }

        public static string AllContentRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/AllContentRss.xml";
        }

        public static string AllTagsListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Tags/AllTagsList.html";
        }

        public static string CameraRollPhotoGalleryUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Photos/Galleries/CameraRoll.html";
        }

        public static async Task<string> ContentUrl(this UserSettings settings, Guid toLink)
        {
            var db = await Db.Context();

            var possiblePost = await db.PostContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possiblePost != null) return settings.PostPageUrl(possiblePost);

            var possibleFile = await db.FileContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possibleFile != null) return settings.FilePageUrl(possibleFile);

            var possiblePhoto = await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possiblePhoto != null) return settings.PhotoPageUrl(possiblePhoto);

            var possibleImage = await db.ImageContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possibleImage != null) return settings.ImagePageUrl(possibleImage);

            var possibleNote = await db.NoteContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possibleNote != null) return settings.NotePageUrl(possibleNote);

            return string.Empty;
        }

        public static GenerationReturn CreateIfItDoesNotExist(this DirectoryInfo directoryInfo)
        {
            if (directoryInfo.Exists)
                return GenerationReturn.Success($"Create If Does Not Exists - {directoryInfo.FullName} Already Exists")
                    .Result;

            try
            {
                directoryInfo.Create();
            }
            catch (Exception e)
            {
                GenerationReturn
                    .Error($"Trying to create Directory {directoryInfo.FullName}  resulted in an Exception.", null, e)
                    .Wait();
            }

            return GenerationReturn.Success($"Created {directoryInfo.FullName}").Result;
        }

        public static string CssMainStyleFileUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/styles.css";
        }

        public static string DailyPhotoGalleryUrl(this UserSettings settings, DateTime galleryDate)
        {
            return $"//{settings.SiteUrl}/Photos/Galleries/Daily/DailyPhotos-{galleryDate:yyyy-MM-dd}.html";
        }

        public static string DefaultContentFormatChoice()
        {
            return Enum.GetNames(typeof(ContentFormatEnum)).First();
        }

        public static async Task EnsureDbIsPresent(this UserSettings settings, IProgress<string> progress)
        {
            var possibleDbFile = new FileInfo(settings.DatabaseFile);

            //if (possibleDbFile.Exists)
            //{
            //    var sc = new ServiceCollection().AddFluentMigratorCore().ConfigureRunner(rb =>
            //            rb.AddSQLite().WithGlobalConnectionString($"Data Source={settings.DatabaseFile}")
            //                .ScanIn(typeof(PointlessWaymarksContext).Assembly).For.Migrations())
            //        .AddLogging(lb => lb.AddFluentMigratorConsole()).BuildServiceProvider(false);

            //    // Instantiate the runner
            //    var runner = sc.GetRequiredService<IMigrationRunner>();

            //    // Execute the migrations
            //    runner.MigrateUp();
            //}

            progress?.Report("Checking for database files...");
            var log = Db.Log().Result;
            await log.Database.EnsureCreatedAsync();
            await EventLogContext.TryWriteStartupMessageToLog(
                $"Ensure Db Is Present - Settings File {SettingsFileName}", "User Settings Utilities");

            var db = Db.Context().Result;
            await db.Database.EnsureCreatedAsync();
        }

        public static string FaviconUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/favicon.ico";
        }

        public static string FileDownloadUrl(this UserSettings settings, FileContent content)
        {
            return $"//{settings.SiteUrl}/Files/{content.Folder}/{content.Slug}/{content.OriginalFileName}";
        }

        public static string FileListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Files/FileList.html";
        }

        public static string FilePageUrl(this UserSettings settings, FileContent content)
        {
            return $"//{settings.SiteUrl}/Files/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string FileRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Files/FileRss.xml";
        }

        public static UserSettingsGenerationValues GenerationValues(this UserSettings settings)
        {
            return (UserSettingsGenerationValues) new UserSettingsGenerationValues().InjectFrom(settings);
        }

        public static string ImageListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Images/ImageList.html";
        }

        public static string ImagePageUrl(this UserSettings settings, ImageContent content)
        {
            return $"//{settings.SiteUrl}/Images/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string ImageRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Images/ImageRss.xml";
        }

        public static string IndexPageUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/index.html";
        }

        public static string LinkListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Links/LinkList.html";
        }

        public static string LinkRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Links/LinkRss.xml";
        }

        public static FileInfo LocalMediaArchiveFileContentFile(this UserSettings settings, FileContent content)
        {
            if (content == null) return null;

            var directory = settings.LocalMediaArchiveFileDirectory();

            return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
        }

        public static DirectoryInfo LocalMediaArchiveFileDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalMediaArchive, "Files"));

            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static FileInfo LocalMediaArchiveImageContentFile(this UserSettings settings, ImageContent content)
        {
            if (content == null) return null;

            var directory = settings.LocalMediaArchiveImageDirectory();

            return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
        }

        public static DirectoryInfo LocalMediaArchiveImageDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalMediaArchive, "Images"));

            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static FileInfo LocalMediaArchivePhotoContentFile(this UserSettings settings, PhotoContent content)
        {
            if (content == null) return null;

            var directory = settings.LocalMediaArchivePhotoDirectory();

            return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
        }

        public static DirectoryInfo LocalMediaArchivePhotoDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalMediaArchive, "Photos"));

            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static FileInfo LocalSiteAllContentListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteRootDirectory;
            return new FileInfo($"{Path.Combine(directory, "AllContentList")}.html");
        }

        public static FileInfo LocalSiteAllContentRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteRootDirectory;
            return new FileInfo($"{Path.Combine(directory, "AllContentRss")}.xml");
        }

        public static FileInfo LocalSiteAllTagsListFileInfo(this UserSettings settings)
        {
            var directory = settings.LocalSiteTagsDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "AllTagsList")}.html");
        }

        public static FileInfo LocalSiteCameraRollPhotoGalleryFileInfo(this UserSettings settings)
        {
            var directory = settings.LocalSitePhotoGalleryDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "CameraRoll")}.html");
        }

        public static DirectoryInfo LocalSiteDailyPhotoGalleryDirectory(this UserSettings settings)
        {
            var photoDirectory =
                new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Photos", "Galleries", "Daily"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static FileInfo LocalSiteDailyPhotoGalleryFileInfo(this UserSettings settings, DateTime galleryDate)
        {
            var directory = settings.LocalSiteDailyPhotoGalleryDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, $"DailyPhotos-{galleryDate:yyyy-MM-dd}")}.html");
        }

        public static DateTime? LocalSiteDailyPhotoGalleryPhotoDateFromFileInfo(this UserSettings settings,
            FileInfo toParse)
        {
            if (toParse == null) return null;
            var name = toParse.Name;
            if (!name.StartsWith("DailyPhotos-")) return null;
            name = name.Replace("DailyPhotos-", "");
            if (!name.EndsWith(".html")) return null;
            name = name.Replace(".html", "");

            if (DateTime.TryParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var parsedDateTime))
                return parsedDateTime;

            return null;
        }

        public static DirectoryInfo LocalSiteDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(settings.LocalSiteRootDirectory);
            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static DirectoryInfo LocalSiteFileContentDirectory(this UserSettings settings, FileContent content,
            bool createDirectoryIfNotFound = true)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteFileDirectory().FullName,
                content.Folder, content.Slug));

            if (contentDirectory.Exists || !createDirectoryIfNotFound) return contentDirectory;

            contentDirectory.Create();
            contentDirectory.Refresh();

            return contentDirectory;
        }

        public static FileInfo LocalSiteFileContentFile(this UserSettings settings, FileContent content)
        {
            var directory = settings.LocalSiteFileContentDirectory(content, false);

            return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
        }

        public static DirectoryInfo LocalSiteFileDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Files"));
            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static FileInfo LocalSiteFileHtmlFile(this UserSettings settings, FileContent content)
        {
            var directory = settings.LocalSiteFileContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static FileInfo LocalSiteFileListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteFileDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "FileList")}.html");
        }

        public static FileInfo LocalSiteFileRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteFileDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "FileRss")}.xml");
        }

        public static DirectoryInfo LocalSiteImageContentDirectory(this UserSettings settings, ImageContent content,
            bool createDirectoryIfNotFound = true)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteImageDirectory().FullName,
                content.Folder, content.Slug));

            if (contentDirectory.Exists || !createDirectoryIfNotFound) return contentDirectory;

            contentDirectory.Create();
            contentDirectory.Refresh();

            return contentDirectory;
        }

        public static FileInfo LocalSiteImageContentFile(this UserSettings settings, ImageContent content)
        {
            var directory = settings.LocalSiteImageContentDirectory(content, false);

            return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
        }

        public static DirectoryInfo LocalSiteImageDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Images"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static FileInfo LocalSiteImageHtmlFile(this UserSettings settings, ImageContent content)
        {
            var directory = settings.LocalSiteImageContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static FileInfo LocalSiteImageListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteImageDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "ImageList")}.html");
        }

        public static FileInfo LocalSiteImageRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteImageDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "ImageRss")}.xml");
        }

        public static FileInfo LocalSiteIndexFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteRootDirectory;
            return new FileInfo($"{Path.Combine(directory, "index")}.html");
        }

        public static DirectoryInfo LocalSiteLinkDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Links"));
            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static FileInfo LocalSiteLinkListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteLinkDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "LinkList")}.html");
        }

        public static FileInfo LocalSiteLinkRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteLinkDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "LinkRss")}.xml");
        }

        public static DirectoryInfo LocalSiteMediaArchiveDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(settings.LocalMediaArchive);
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static DirectoryInfo LocalSiteNoteContentDirectory(this UserSettings settings, NoteContent content,
            bool createDirectoryIfNotFound = true)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteNoteDirectory().FullName,
                content.Folder));

            if (contentDirectory.Exists || !createDirectoryIfNotFound) return contentDirectory;

            contentDirectory.Create();
            contentDirectory.Refresh();

            return contentDirectory;
        }

        public static DirectoryInfo LocalSiteNoteDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Notes"));
            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static FileInfo LocalSiteNoteHtmlFile(this UserSettings settings, NoteContent content)
        {
            var directory = settings.LocalSiteNoteContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static FileInfo LocalSiteNoteListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteNoteDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "NoteList")}.html");
        }

        public static FileInfo LocalSiteNoteRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteNoteDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "NoteRss")}.xml");
        }

        public static DirectoryInfo LocalSitePhotoContentDirectory(this UserSettings settings, PhotoContent content,
            bool createDirectoryIfNotFound = true)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSitePhotoDirectory().FullName,
                content.Folder, content.Slug));

            if (contentDirectory.Exists || !createDirectoryIfNotFound) return contentDirectory;

            contentDirectory.Create();
            contentDirectory.Refresh();

            return contentDirectory;
        }

        public static DirectoryInfo LocalSitePhotoDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Photos"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static DirectoryInfo LocalSitePhotoGalleryDirectory(this UserSettings settings)
        {
            var photoDirectory =
                new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Photos", "Galleries"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static FileInfo LocalSitePhotoHtmlFile(this UserSettings settings, PhotoContent content)
        {
            var directory = settings.LocalSitePhotoContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static FileInfo LocalSitePhotoListFile(this UserSettings settings)
        {
            var directory = settings.LocalSitePhotoDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "PhotoList")}.html");
        }

        public static FileInfo LocalSitePhotoRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSitePhotoDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "PhotoRss")}.xml");
        }

        public static DirectoryInfo LocalSitePointContentDirectory(this UserSettings settings, PointContent content,
            bool createDirectoryIfNotFound = true)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSitePointDirectory().FullName,
                content.Folder, content.Slug));

            if (contentDirectory.Exists || !createDirectoryIfNotFound) return contentDirectory;

            contentDirectory.Create();
            contentDirectory.Refresh();

            return contentDirectory;
        }

        public static DirectoryInfo LocalSitePointDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Points"));
            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static FileInfo LocalSitePointHtmlFile(this UserSettings settings, PointContent content)
        {
            var directory = settings.LocalSitePointContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static FileInfo LocalSitePointListFile(this UserSettings settings)
        {
            var directory = settings.LocalSitePointDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "PointList")}.html");
        }

        public static FileInfo LocalSitePointRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSitePointDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "PointRss")}.xml");
        }

        public static DirectoryInfo LocalSitePostContentDirectory(this UserSettings settings, PostContent content,
            bool createDirectoryIfNotFound = true)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSitePostDirectory().FullName,
                content.Folder, content.Slug));

            if (contentDirectory.Exists || !createDirectoryIfNotFound) return contentDirectory;

            contentDirectory.Create();
            contentDirectory.Refresh();

            return contentDirectory;
        }


        public static DirectoryInfo LocalSitePostDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Posts"));
            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static FileInfo LocalSitePostHtmlFile(this UserSettings settings, PostContent content)
        {
            var directory = settings.LocalSitePostContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static FileInfo LocalSitePostListFile(this UserSettings settings)
        {
            var directory = settings.LocalSitePostDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "PostList")}.html");
        }

        public static FileInfo LocalSitePostRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSitePostDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "PostRss")}.xml");
        }

        public static FileInfo LocalSiteRssIndexFeedListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteRootDirectory;
            return new FileInfo($"{Path.Combine(directory, "RssIndexFeed")}.xml");
        }

        public static FileInfo LocalSiteTagListFileInfo(this UserSettings settings, string tag)
        {
            var directory = settings.LocalSiteTagsDirectory();
            var sluggedTag = SlugUtility.Create(true, tag, 200);
            return new FileInfo($"{Path.Combine(directory.FullName, $"TagList-{sluggedTag}")}.html");
        }


        public static DirectoryInfo LocalSiteTagsDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Tags"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static string NoteListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Notes/NoteList.html";
        }

        public static string NotePageUrl(this UserSettings settings, NoteContent content)
        {
            return $"//{settings.SiteUrl}/Notes/{content.Folder}/{content.Slug}.html";
        }

        public static string NoteRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Notes/NoteRss.xml";
        }

        public static async Task<string> PageUrl(this UserSettings settings, Guid contentGuid)
        {
            var db = await Db.Context();
            var content = await db.ContentFromContentId(contentGuid);

            return content switch
            {
                FileContent c => settings.FilePageUrl(c),
                ImageContent c => settings.ImagePageUrl(c),
                LinkContent _ => settings.LinkListUrl(),
                NoteContent c => settings.NotePageUrl(c),
                PhotoContent c => settings.PhotoPageUrl(c),
                PostContent c => settings.PostPageUrl(c),
                _ => throw new DataException("Content not Found")
            };
        }

        public static string PhotoListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Photos/PhotoList.html";
        }

        public static string PhotoPageUrl(this UserSettings settings, PhotoContent content)
        {
            return $"//{settings.SiteUrl}/Photos/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string PhotoRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Photos/PhotoRss.xml";
        }

        public static string PicturePageUrl(this UserSettings settings, Guid contentGuid)
        {
            var db = Db.Context().Result;

            var possiblePhotoContent = db.PhotoContents.SingleOrDefault(x => x.ContentId == contentGuid);
            if (possiblePhotoContent != null) return settings.PhotoPageUrl(possiblePhotoContent);

            var possibleImageContent = db.ImageContents.SingleOrDefault(x => x.ContentId == contentGuid);
            if (possibleImageContent != null) return settings.ImagePageUrl(possibleImageContent);

            return string.Empty;
        }

        public static string PointPageUrl(this UserSettings settings, PointContent content)
        {
            return $"//{settings.SiteUrl}/Points/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string PointsListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Points/PointList.html";
        }

        public static string PointsRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Points/PointRss.xml";
        }

        public static string PostPageUrl(this UserSettings settings, PostContent content)
        {
            return $"//{settings.SiteUrl}/Posts/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string PostsListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Posts/PostList.html";
        }

        public static string PostsRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Posts/PostRss.xml";
        }

        public static async Task<UserSettings> ReadSettings(IProgress<string> progress)
        {
            var currentFile = SettingsFile();

            if (!currentFile.Exists)
                throw new InvalidDataException($"Settings file {currentFile.FullName} doesn't exist?");

            if (currentFile.Directory == null)
                throw new InvalidDataException($"Settings file {currentFile.FullName} doesn't have a valid directory?");

            UserSettings readResult;

            progress?.Report($"Reading and deserializing {currentFile.FullName}");

            await using (var fs = new FileStream(currentFile.FullName, FileMode.Open, FileAccess.Read))
            {
                readResult = await JsonSerializer.DeserializeAsync<UserSettings>(fs);
            }

            var timeStampForMissingValues = $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss-fff}";

            var hasUpdates = false;

            progress?.Report("Checking for missing values in settings...");

            if (string.IsNullOrWhiteSpace(readResult.LocalSiteRootDirectory))
            {
                //This could fail for all kinds of interesting reasons but for the purposes of this program I am not sure that
                //industrial strength name collision avoidance is needed
                var newRootDirectory =
                    new DirectoryInfo(Path.Combine(currentFile.Directory.FullName, timeStampForMissingValues));

                newRootDirectory.CreateIfItDoesNotExist();

                readResult.LocalSiteRootDirectory = newRootDirectory.FullName;

                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.DatabaseFile))
            {
                //This could fail for all kinds of interesting reasons but for the purposes of this program I am not sure that
                //industrial strength name collision avoidance is needed
                readResult.DatabaseFile = $"PointlessWaymarksData-{timeStampForMissingValues}.db";

                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.LocalSiteRootDirectory))
            {
                var newLocalSiteRoot = new DirectoryInfo(Path.Combine(currentFile.Directory.FullName,
                    timeStampForMissingValues, $"PointlessWaymarks-Site-{timeStampForMissingValues}"));

                if (!newLocalSiteRoot.Exists) newLocalSiteRoot.Create();
                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.LocalMediaArchive))
            {
                var newMediaArchive = new DirectoryInfo(Path.Combine(currentFile.Directory.FullName,
                    timeStampForMissingValues, $"PointlessWaymarks-MediaArchive-{timeStampForMissingValues}"));

                if (!newMediaArchive.Exists) newMediaArchive.Create();
                readResult.LocalMediaArchive = newMediaArchive.FullName;
                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.DefaultCreatedBy))
            {
                readResult.DefaultCreatedBy = "Pointless Waymarks CMS";
                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.SiteName))
            {
                readResult.SiteName = "New Site";
                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.SiteUrl))
            {
                readResult.SiteName = "localhost.com";
                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.SiteKeywords))
            {
                readResult.SiteKeywords = "new,site";
                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.SiteSummary))
            {
                readResult.SiteSummary = "A new site.";
                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.SiteAuthors))
            {
                readResult.SiteAuthors = "Pointless Waymarks CMS";
                hasUpdates = true;
            }

            if (string.IsNullOrWhiteSpace(readResult.SiteEmailTo))
            {
                readResult.SiteEmailTo = "nothing@nowhere.com";
                hasUpdates = true;
            }

            if (hasUpdates)
            {
                progress?.Report("Found missing values - writing defaults back to settings.");
                await WriteSettings(readResult);
            }

            return readResult;
        }

        public static string RssIndexFeedUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/RssIndexFeed.xml";
        }

        public static FileInfo SettingsFile()
        {
            return new FileInfo(Path.Combine(StorageDirectory().FullName, SettingsFileName));
        }

        /// <summary>
        /// </summary>
        /// <param name="userFilename">File Name for the </param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static async Task<UserSettings> SetupNewSite(string userFilename, IProgress<string> progress)
        {
            if (!FolderFileUtility.IsValidWindowsFileSystemFilename(userFilename))
                throw new InvalidDataException("New site input must be a valid filename.");

            var newSettings = new UserSettings();

            var rootDirectory = new DirectoryInfo(Path.Combine(StorageDirectory().FullName, userFilename));

            progress?.Report("Creating new settings - looking for home...");

            var fileNumber = 1;

            while (rootDirectory.Exists)
            {
                rootDirectory =
                    new DirectoryInfo(Path.Combine(StorageDirectory().FullName, $"{userFilename}-{fileNumber}"));
                rootDirectory.Refresh();
                progress?.Report($"Trying {rootDirectory.FullName}...");
                fileNumber++;
            }

            rootDirectory.Create();

            var siteRoot = new DirectoryInfo(Path.Combine(rootDirectory.FullName, "GeneratedSite"));
            newSettings.LocalSiteRootDirectory = siteRoot.FullName;

            progress?.Report($"Local Site Root set to {siteRoot.FullName}");

            newSettings.DatabaseFile =
                Path.Combine(rootDirectory.FullName, $"PointlessWaymarksCmsDatabase-{userFilename}.db");

            var mediaDirectory = new DirectoryInfo(Path.Combine(rootDirectory.FullName, "MediaArchive"));
            newSettings.LocalMediaArchive = mediaDirectory.FullName;

            progress?.Report("Adding fake default values...");

            newSettings.DefaultCreatedBy = "Pointless Waymarks CMS";
            newSettings.SiteName = userFilename;
            newSettings.SiteUrl = "localhost.com";
            newSettings.SiteKeywords = "new,site";
            newSettings.SiteSummary = "A new site.";
            newSettings.SiteAuthors = "Pointless Waymarks CMS";
            newSettings.SiteEmailTo = "nothing@nowhere.com";
            newSettings.LatitudeDefault = 32.443131;
            newSettings.LongitudeDefault = -110.788429;

            SettingsFileName =
                Path.Combine(rootDirectory.FullName, $"PointlessWaymarksCmsSettings-{userFilename}.json");

            progress?.Report("Writing Settings");

            await WriteSettings(newSettings);

            progress?.Report("Setting up directory structure.");

            newSettings.VerifyOrCreateAllTopLevelFolders(progress);

            return newSettings;
        }

        public static DirectoryInfo StorageDirectory()
        {
            var storageDirectory =
                new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Pointless Waymarks Cms"));

            if (!storageDirectory.Exists) storageDirectory.Create();

            storageDirectory.Refresh();

            return storageDirectory;
        }

        public static string StylesCssFromLocalSiteRootDirectory()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var possibleFile = new FileInfo(Path.Combine(settings.LocalSiteRootDirectory, "styles.css"));

            if (!possibleFile.Exists) return string.Empty;

            return File.ReadAllText(possibleFile.FullName);
        }

        public static string TagPageUrl(this UserSettings settings, string tag)
        {
            var sluggedTag = SlugUtility.Create(true, tag, 200);
            return $"//{settings.SiteUrl}/Tags/TagList-{sluggedTag}.html";
        }

        public static DirectoryInfo TempStorageDirectory()
        {
            var storageDirectory = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pointless Waymarks Cms",
                "TemporaryFiles"));

            if (!storageDirectory.Exists) storageDirectory.Create();

            storageDirectory.Refresh();

            return storageDirectory;
        }

        public static (bool, string) ValidateLocalMediaArchive()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            if (string.IsNullOrWhiteSpace(settings.LocalMediaArchive))
                return (false, "No Local File Root User Setting Found");

            try
            {
                var directory = new DirectoryInfo(settings.LocalMediaArchive);
                if (!directory.Exists) directory.Create();
                directory.Refresh();
            }
            catch (Exception e)
            {
                return (false, $"Trouble with Local Media Archive Directory - {e.Message}");
            }

            return (true, string.Empty);
        }

        public static (bool, string) ValidateLocalSiteRootDirectory()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            if (string.IsNullOrWhiteSpace(settings.LocalSiteRootDirectory))
                return (false, "No Local File Root User Setting Found");

            try
            {
                var directory = new DirectoryInfo(settings.LocalSiteRootDirectory);
                if (!directory.Exists) directory.Create();
                directory.Refresh();
            }
            catch (Exception e)
            {
                return (false, $"Trouble with Local File Root Directory - {e.Message}");
            }

            return (true, string.Empty);
        }


        public static async Task WriteSettings(this UserSettings toWrite)
        {
            var currentFile = SettingsFile();
            await File.WriteAllTextAsync(currentFile.FullName, JsonSerializer.Serialize(toWrite));
        }
    }
}