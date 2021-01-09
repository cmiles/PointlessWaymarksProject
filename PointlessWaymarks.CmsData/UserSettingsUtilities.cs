using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData
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

            //!!Content Type List!!

            var possibleFile = await db.FileContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possibleFile != null) return settings.FilePageUrl(possibleFile);

            var possibleGeoJson = await db.GeoJsonContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possibleGeoJson != null) return settings.GeoJsonPageUrl(possibleGeoJson);

            var possibleImage = await db.ImageContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possibleImage != null) return settings.ImagePageUrl(possibleImage);

            var possibleLine = await db.LineContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possibleLine != null) return settings.LinePageUrl(possibleLine);

            var possibleLink = await db.LinkContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possibleLink != null) return settings.LinkListUrl();

            var possibleNote = await db.NoteContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possibleNote != null) return settings.NotePageUrl(possibleNote);

            var possiblePhoto = await db.PhotoContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possiblePhoto != null) return settings.PhotoPageUrl(possiblePhoto);

            var possiblePoint = await db.PointContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possiblePoint != null) return settings.PointPageUrl(possiblePoint);

            var possiblePost = await db.PostContents.SingleOrDefaultAsync(x => x.ContentId == toLink);
            if (possiblePost != null) return settings.PostPageUrl(possiblePost);

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
            return $"//{settings.SiteUrl}/style.css";
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
            //TODO: Re-enable migrations if any to apply
            //
            //var possibleDbFile = new FileInfo(settings.DatabaseFile);
            //
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

        public static string GeoJsonListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/GeoJson/GeoJsonList.html";
        }

        public static string GeoJsonPageUrl(this UserSettings settings, GeoJsonContent content)
        {
            return $"//{settings.SiteUrl}/GeoJson/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string GeoJsonRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/GeoJson/GeoJsonRss.xml";
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

        public static string LinePageUrl(this UserSettings settings, LineContent content)
        {
            return $"//{settings.SiteUrl}/Lines/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string LinesListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Lines/LineList.html";
        }

        public static string LinesRssUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Lines/LineRss.xml";
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
            var directory = new DirectoryInfo(Path.Combine(settings.LocalMediaArchive, "Files"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static FileInfo LocalMediaArchiveImageContentFile(this UserSettings settings, ImageContent content)
        {
            if (content == null) return null;

            var directory = settings.LocalMediaArchiveImageDirectory();

            return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
        }

        public static DirectoryInfo LocalMediaArchiveImageDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalMediaArchive, "Images"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalMediaArchiveLogsDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalMediaArchive, "Logs"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static FileInfo LocalMediaArchivePhotoContentFile(this UserSettings settings, PhotoContent content)
        {
            if (content == null) return null;

            var directory = settings.LocalMediaArchivePhotoDirectory();

            return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
        }

        public static DirectoryInfo LocalMediaArchivePhotoDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalMediaArchive, "Photos"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
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
            var directory =
                new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Photos", "Galleries", "Daily"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
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
            var directory = new DirectoryInfo(settings.LocalSiteRootDirectory);
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSiteFileContentDirectory(this UserSettings settings, FileContent content,
            bool createDirectoryIfNotFound = true)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteFileDirectory().FullName, content.Folder,
                content.Slug));

            if (directory.Exists || !createDirectoryIfNotFound) return directory;

            directory.Create();
            directory.Refresh();

            return directory;
        }

        public static FileInfo LocalSiteFileContentFile(this UserSettings settings, FileContent content)
        {
            var directory = settings.LocalSiteFileContentDirectory(content, false);

            return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
        }

        public static DirectoryInfo LocalSiteFileDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Files"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
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

        public static DirectoryInfo LocalSiteGeoJsonContentDirectory(this UserSettings settings, GeoJsonContent content,
            bool createDirectoryIfNotFound = true)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteGeoJsonDirectory().FullName,
                content.Folder, content.Slug));

            if (directory.Exists || !createDirectoryIfNotFound) return directory;

            directory.Create();
            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSiteGeoJsonDataDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(LocalSiteGeoJsonDirectory(settings).FullName, "Data"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSiteGeoJsonDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "GeoJson"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static FileInfo LocalSiteGeoJsonHtmlFile(this UserSettings settings, GeoJsonContent content)
        {
            var directory = settings.LocalSiteGeoJsonContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static FileInfo LocalSiteGeoJsonListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteGeoJsonDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "GeoJsonList")}.html");
        }

        public static FileInfo LocalSiteGeoJsonRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteGeoJsonDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "GeoJsonRss")}.xml");
        }

        public static DirectoryInfo LocalSiteImageContentDirectory(this UserSettings settings, ImageContent content,
            bool createDirectoryIfNotFound = true)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteImageDirectory().FullName, content.Folder,
                content.Slug));

            if (directory.Exists || !createDirectoryIfNotFound) return directory;

            directory.Create();
            directory.Refresh();

            return directory;
        }

        public static FileInfo LocalSiteImageContentFile(this UserSettings settings, ImageContent content)
        {
            var directory = settings.LocalSiteImageContentDirectory(content, false);

            return new FileInfo(Path.Combine(directory.FullName, content.OriginalFileName));
        }

        public static DirectoryInfo LocalSiteImageDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Images"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
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

        public static DirectoryInfo LocalSiteLineContentDirectory(this UserSettings settings, LineContent content,
            bool createDirectoryIfNotFound = true)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteLineDirectory().FullName, content.Folder,
                content.Slug));

            if (directory.Exists || !createDirectoryIfNotFound) return directory;

            directory.Create();
            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSiteLineDataDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(LocalSiteLineDirectory(settings).FullName, "Data"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSiteLineDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Lines"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static FileInfo LocalSiteLineHtmlFile(this UserSettings settings, LineContent content)
        {
            var directory = settings.LocalSiteLineContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static FileInfo LocalSiteLineListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteLineDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "LineList")}.html");
        }

        public static FileInfo LocalSiteLineRssFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteLineDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "LineRss")}.xml");
        }

        public static DirectoryInfo LocalSiteLinkDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Links"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
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

        public static DirectoryInfo LocalSiteMapComponentDataDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(LocalSiteMapComponentDirectory(settings).FullName, "Data"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static FileInfo LocalSiteMapComponentDataFile(this UserSettings settings, Guid contentId)
        {
            var directory = settings.LocalSiteMapComponentDataDirectory();
            return new FileInfo(
                $"{Path.Combine(directory.FullName, Names.MapComponentContentPrefix, contentId.ToString())}.json");
        }

        public static DirectoryInfo LocalSiteMapComponentDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Maps"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSiteMediaArchiveDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(settings.LocalMediaArchive);

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSiteNoteContentDirectory(this UserSettings settings, NoteContent content,
            bool createDirectoryIfNotFound = true)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteNoteDirectory().FullName, content.Folder));

            if (directory.Exists || !createDirectoryIfNotFound) return directory;

            directory.Create();
            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSiteNoteDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Notes"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
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
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSitePhotoDirectory().FullName, content.Folder,
                content.Slug));

            if (directory.Exists || !createDirectoryIfNotFound) return directory;

            directory.Create();
            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSitePhotoDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Photos"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSitePhotoGalleryDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Photos", "Galleries"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
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
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSitePointDirectory().FullName, content.Folder,
                content.Slug));

            if (directory.Exists || !createDirectoryIfNotFound) return directory;

            directory.Create();
            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSitePointContentDirectory(this UserSettings settings, PointContentDto content,
            bool createDirectoryIfNotFound = true)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSitePointDirectory().FullName, content.Folder,
                content.Slug));

            if (directory.Exists || !createDirectoryIfNotFound) return directory;

            directory.Create();
            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSitePointDataDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(LocalSitePointDirectory(settings).FullName, "Data"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static FileInfo LocalSitePointDataFile(this UserSettings settings)
        {
            var directory = settings.LocalSitePointDataDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "pointdata")}.json");
        }

        public static DirectoryInfo LocalSitePointDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Points"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }


        public static FileInfo LocalSitePointHtmlFile(this UserSettings settings, PointContent content)
        {
            var directory = settings.LocalSitePointContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static FileInfo LocalSitePointHtmlFile(this UserSettings settings, PointContentDto content)
        {
            var directory =
                settings.LocalSitePointContentDirectory(Db.PointContentDtoToPointContentAndDetails(content).content,
                    false);
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
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSitePostDirectory().FullName, content.Folder,
                content.Slug));

            if (directory.Exists || !createDirectoryIfNotFound) return directory;

            directory.Create();
            directory.Refresh();

            return directory;
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

        public static DirectoryInfo LocalSiteScriptsDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalMediaArchive, "Scripts"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo LocalSiteSiteResourcesDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "SiteResources"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static FileInfo LocalSiteTagListFileInfo(this UserSettings settings, string tag)
        {
            var directory = settings.LocalSiteTagsDirectory();
            var sluggedTag = SlugUtility.Create(true, tag, 200);
            return new FileInfo($"{Path.Combine(directory.FullName, $"TagList-{sluggedTag}")}.html");
        }


        public static DirectoryInfo LocalSiteTagsDirectory(this UserSettings settings)
        {
            var directory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Tags"));
            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
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

            //!!Content Type List!!

            return content switch
            {
                FileContent c => settings.FilePageUrl(c),
                GeoJsonContent c => settings.GeoJsonPageUrl(c),
                ImageContent c => settings.ImagePageUrl(c),
                LineContent c => settings.LinePageUrl(c),
                LinkContent _ => settings.LinkListUrl(),
                NoteContent c => settings.NotePageUrl(c),
                PhotoContent c => settings.PhotoPageUrl(c),
                PointContent c => settings.PointPageUrl(c),
                PointContentDto c => settings.PointPageUrl(Db.PointContentDtoToPointContentAndDetails(c).content),
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

        public static string PointDataUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Points/Data/pointdata.json";
        }

        public static string PointPageUrl(this UserSettings settings, PointContent content)
        {
            return $"//{settings.SiteUrl}/Points/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string PointPageUrl(this UserSettings settings, PointContentDto content)
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
                readResult.SiteUrl = "localhost.com";
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

            if (readResult.SettingsId == Guid.Empty)
            {
                readResult.SettingsId = Guid.NewGuid();
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

        public static string SearchListJavascriptUrl(this UserSettings settings)
        {
            return
                $"{UserSettingsSingleton.CurrentSettings().SiteResourcesUrl()}pointless-waymarks-content-list-search.js";
        }

        public static FileInfo SettingsFile()
        {
            return new(Path.Combine(StorageDirectory().FullName, SettingsFileName));
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
            newSettings.SettingsId = Guid.NewGuid();

            SettingsFileName =
                Path.Combine(rootDirectory.FullName, $"PointlessWaymarksCmsSettings-{userFilename}.json");

            progress?.Report("Writing Settings");

            await WriteSettings(newSettings);

            progress?.Report("Setting up directory structure.");

            newSettings.VerifyOrCreateAllTopLevelFolders();
            await newSettings.EnsureDbIsPresent(progress);

            await FileManagement.WriteFavIconToGeneratedSite(progress);
            await FileManagement.WriteStylesCssToGeneratedSite(progress);
            await FileManagement.WriteSiteResourcesToGeneratedSite(progress);

            return newSettings;
        }

        public static string SiteResourcesUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/SiteResources/";
        }

        public static RegionEndpoint SiteS3BucketEndpoint(this UserSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SiteS3BucketRegion)) return null;

            return RegionEndpoint.EnumerableAllRegions.SingleOrDefault(x =>
                x.SystemName == settings.SiteS3BucketRegion);
        }

        public static DirectoryInfo StorageDirectory()
        {
            var directory =
                new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Pointless Waymarks Cms"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static string StylesCssFromLocalSiteRootDirectory()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var possibleFile = new FileInfo(Path.Combine(settings.LocalSiteRootDirectory, "style.css"));

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
            var directory = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pointless Waymarks Cms",
                "TemporaryFiles"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
        }

        public static DirectoryInfo TempStorageHtmlDirectory()
        {
            var directory = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pointless Waymarks Cms",
                "TemporaryFiles", "Html"));

            if (!directory.Exists) directory.Create();

            directory.Refresh();

            return directory;
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