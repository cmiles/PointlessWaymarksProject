using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData
{
    public static class UserSettingsUtilities
    {
        public static string CssMainStyleFileUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/styles.css";
        }

        public static string FaviconUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/favicon.ico";
        }

        public static string FilePageUrl(this UserSettings settings, FileContent content)
        {
            return $"//{settings.SiteUrl}/Files/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string ImagePageUrl(this UserSettings settings, ImageContent content)
        {
            return $"//{settings.SiteUrl}/Images/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string IndexPageUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/index.html";
        }

        public static DirectoryInfo LocalMasterMediaArchiveFileDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalMasterMediaArchive, "Files"));

            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static DirectoryInfo LocalMasterMediaArchiveImageDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalMasterMediaArchive, "Images"));

            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }


        public static DirectoryInfo LocalMasterMediaArchivePhotoDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalMasterMediaArchive, "Images"));

            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static DirectoryInfo LocalSiteDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(settings.LocalSiteRootDirectory);
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static DirectoryInfo LocalSiteFileContentDirectory(this UserSettings settings, FileContent content)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteFileDirectory().FullName,
                content.Folder, content.Slug));
            if (!contentDirectory.Exists) contentDirectory.Create();

            contentDirectory.Refresh();

            return contentDirectory;
        }

        public static DirectoryInfo LocalSiteFileDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Files"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static DirectoryInfo LocalSiteImageContentDirectory(this UserSettings settings, ImageContent content)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteImageDirectory().FullName,
                content.Folder, content.Slug));
            if (!contentDirectory.Exists) contentDirectory.Create();

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

        public static DirectoryInfo LocalSiteMasterMediaArchiveDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(settings.LocalMasterMediaArchive);
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static DirectoryInfo LocalSitePhotoContentDirectory(this UserSettings settings, PhotoContent content)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSitePhotoDirectory().FullName,
                content.Folder, content.Slug));
            if (!contentDirectory.Exists) contentDirectory.Create();

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

        public static DirectoryInfo LocalSitePostContentDirectory(this UserSettings settings, PostContent content)
        {
            var contentDirectory = new DirectoryInfo(Path.Combine(settings.LocalSitePostDirectory().FullName,
                content.Folder, content.Slug));
            if (!contentDirectory.Exists) contentDirectory.Create();

            contentDirectory.Refresh();

            return contentDirectory;
        }

        public static DirectoryInfo LocalSitePostDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Posts"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static string PhotoPageUrl(this UserSettings settings, PhotoContent content)
        {
            return $"//{settings.SiteUrl}/Photos/{content.Folder}/{content.Slug}/{content.Slug}.html";
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

        public static async Task<UserSettings> ReadSettings()
        {
            var currentFile = SettingsFile();
            if (!currentFile.Exists)
            {
                await WriteSettings(new UserSettings());
                currentFile.Refresh();
            }

            return await JsonSerializer.DeserializeAsync<UserSettings>(File.OpenRead(currentFile.FullName));
        }

        public static FileInfo SettingsFile()
        {
            return new FileInfo(Path.Combine(StorageDirectory().FullName, "HikeLemmonWorkshopSettings.json"));
        }

        public static DirectoryInfo StorageDirectory()
        {
            var storageDirectory =
                new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "HikeLemmon Workshop"));

            if (!storageDirectory.Exists) storageDirectory.Create();

            storageDirectory.Refresh();

            return storageDirectory;
        }

        public static string StylesCssFromLocalSiteRootDirectory()
        {
            var settings = ReadSettings().Result;

            var possibleFile = new FileInfo(Path.Combine(settings.LocalSiteRootDirectory, "styles.css"));

            if (!possibleFile.Exists) return string.Empty;

            return File.ReadAllText(possibleFile.FullName);
        }

        public static async Task<(bool, string)> ValidateLocalMasterMediaArchive()
        {
            var settings = await ReadSettings();

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
            var settings = await ReadSettings();

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

        public static void VerifyAndCreate()
        {
            ReadSettings().Wait();
        }

        public static async Task WriteSettings(UserSettings toWrite)
        {
            var currentFile = SettingsFile();
            await File.WriteAllTextAsync(currentFile.FullName, JsonSerializer.Serialize(toWrite));
        }
    }
}