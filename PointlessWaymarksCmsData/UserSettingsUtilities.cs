using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Models;

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

        public static void CreateIfItDoesntExist(this DirectoryInfo directoryInfo)
        {
            if (directoryInfo.Exists) return;
            directoryInfo.Create();
        }

        public static string CssMainStyleFileUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/styles.css";
        }

        public static string DailyPhotoGalleryUrl(this UserSettings settings, DateTime galleryDate)
        {
            return $"//{settings.SiteUrl}/Photos/Galleries/Daily/DailyPhotos-{galleryDate:yyyy-MM-dd}.html";
        }

        public static string CameraRollPhotoGalleryUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Photos/Galleries/CameraRoll.html";
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

        public static DirectoryInfo LocalMasterMediaArchiveFileDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalMasterMediaArchive, "Files"));

            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }

        public static DirectoryInfo LocalMasterMediaArchiveImageDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalMasterMediaArchive, "Images"));

            if (!localDirectory.Exists) localDirectory.Create();

            localDirectory.Refresh();

            return localDirectory;
        }


        public static DirectoryInfo LocalMasterMediaArchivePhotoDirectory(this UserSettings settings)
        {
            var localDirectory = new DirectoryInfo(Path.Combine(settings.LocalMasterMediaArchive, "Photos"));

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

        public static DirectoryInfo LocalSiteDailyPhotoGalleryDirectory(this UserSettings settings)
        {
            var photoDirectory =
                new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Photos", "Galleries", "Daily"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static FileInfo LocalSiteDailyPhotoGalleryFileInfo(this UserSettings settings, DateTime galleryDate,
            bool createDirectoryIfNotFound = true)
        {
            var directory = settings.LocalSiteDailyPhotoGalleryDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, $"DailyPhotos-{galleryDate:yyyy-MM-dd}")}.html");
        }

        public static FileInfo LocalSiteCameraRollPhotoGalleryFileInfo(this UserSettings settings, 
            bool createDirectoryIfNotFound = true)
        {
            var directory = settings.LocalSitePhotoGalleryDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "CameraRoll")}.html");
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

        public static DirectoryInfo LocalSiteMasterMediaArchiveDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(settings.LocalMasterMediaArchive);
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

                newRootDirectory.CreateIfItDoesntExist();

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

            if (string.IsNullOrWhiteSpace(readResult.LocalMasterMediaArchive))
            {
                var newMediaArchive = new DirectoryInfo(Path.Combine(currentFile.Directory.FullName,
                    timeStampForMissingValues, $"PointlessWaymarks-MediaArchive-{timeStampForMissingValues}"));

                if (!newMediaArchive.Exists) newMediaArchive.Create();
                readResult.LocalMasterMediaArchive = newMediaArchive.FullName;
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

        public static async Task<UserSettings> SetupNewSite(string userFilename, IProgress<string> progress)
        {
            if (!FolderFileUtility.IsValidFilename(userFilename))
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
            newSettings.LocalMasterMediaArchive = mediaDirectory.FullName;

            progress?.Report("Adding fake default values...");

            newSettings.DefaultCreatedBy = "Pointless Waymarks CMS";
            newSettings.SiteName = userFilename;
            newSettings.SiteUrl = "localhost.com";
            newSettings.SiteKeywords = "new,site";
            newSettings.SiteSummary = "A new site.";
            newSettings.SiteAuthors = "Pointless Waymarks CMS";
            newSettings.SiteEmailTo = "nothing@nowhere.com";

            SettingsFileName =
                Path.Combine(rootDirectory.FullName, $"PointlessWaymarksCmsSettings-{userFilename}.json");

            progress?.Report("Writing Settings");

            await WriteSettings(newSettings);

            progress?.Report("Setting up directory structure.");

            VerifyOrCreateAllFolders(newSettings);

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

        public static DirectoryInfo TempStorageDirectory()
        {
            var storageDirectory = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pointless Waymarks Cms",
                "TemporaryFiles"));

            if (!storageDirectory.Exists) storageDirectory.Create();

            storageDirectory.Refresh();

            return storageDirectory;
        }

        public static async Task<(bool, string)> ValidateLocalMasterMediaArchive()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            if (string.IsNullOrWhiteSpace(settings.LocalMasterMediaArchive))
                return (false, "No Local File Root User Setting Found");

            try
            {
                var directory = new DirectoryInfo(settings.LocalMasterMediaArchive);
                if (!directory.Exists) directory.Create();
                directory.Refresh();
            }
            catch (Exception e)
            {
                return (false, "Trouble with Local Master Media Archive Directory.");
            }

            return (true, string.Empty);
        }

        public static async Task<(bool, string)> ValidateLocalSiteRootDirectory()
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
                return (false, "Trouble with Local File Root Directory.");
            }

            return (true, string.Empty);
        }

        public static void VerifyOrCreateAllFolders(this UserSettings settings)
        {
            settings.VerifyOrCreateMediaFolders();
            settings.VerifyOrCreateLocalSiteFolders();
        }

        public static void VerifyOrCreateLocalSiteFolders(this UserSettings settings)
        {
            settings.LocalSiteDirectory().CreateIfItDoesntExist();
            settings.LocalSitePhotoDirectory().CreateIfItDoesntExist();
            settings.LocalSitePhotoGalleryDirectory().CreateIfItDoesntExist();
            settings.LocalSiteFileDirectory().CreateIfItDoesntExist();
            settings.LocalSiteImageDirectory().CreateIfItDoesntExist();
            settings.LocalSiteNoteDirectory().CreateIfItDoesntExist();
            settings.LocalSitePostDirectory().CreateIfItDoesntExist();
            settings.LocalSiteLinkDirectory().CreateIfItDoesntExist();
            settings.LocalSiteNoteDirectory().CreateIfItDoesntExist();
        }

        public static void VerifyOrCreateMediaFolders(this UserSettings settings)
        {
            settings.LocalSiteMasterMediaArchiveDirectory().CreateIfItDoesntExist();
            settings.LocalMasterMediaArchivePhotoDirectory().CreateIfItDoesntExist();
            settings.LocalMasterMediaArchiveImageDirectory().CreateIfItDoesntExist();
            settings.LocalMasterMediaArchiveFileDirectory().CreateIfItDoesntExist();
        }

        public static async Task WriteSettings(UserSettings toWrite)
        {
            var currentFile = SettingsFile();
            await File.WriteAllTextAsync(currentFile.FullName, JsonSerializer.Serialize(toWrite));
        }
    }
}