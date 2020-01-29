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
        public static string CssMainStyleFileUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/styles.css";
        }

        public static string FaviconUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/favicon.ico";
        }

        public static string FileDownloadUrl(this UserSettings settings, FileContent content)
        {
            return $"//{settings.SiteUrl}/Files/{content.Folder}/{content.Slug}/{content.OriginalFileName}";
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
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalMasterMediaArchive, "Photos"));

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

        public static DirectoryInfo LocalSiteFileDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Files"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static FileInfo LocalSiteFileHtmlFile(this UserSettings settings, FileContent content)
        {
            var directory = settings.LocalSiteFileContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
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

        public static DirectoryInfo LocalSiteMasterMediaArchiveDirectory(this UserSettings settings)
        {
            var photoDirectory = new DirectoryInfo(settings.LocalMasterMediaArchive);
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
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

        public static FileInfo LocalSiteAllContentListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteRootDirectory;
            return new FileInfo($"{Path.Combine(directory, "AllContentList")}.html");
        }

        public static FileInfo LocalSiteImageListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteImageDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "ImageList")}.html");
        }

        public static FileInfo LocalSiteFileListFile(this UserSettings settings)
        {
            var directory = settings.LocalSiteFileDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "FileList")}.html");
        }

        public static FileInfo LocalSitePostListFile(this UserSettings settings)
        {
            var directory = settings.LocalSitePostDirectory();
            return new FileInfo($"{Path.Combine(directory.FullName, "PostList")}.html");
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
            var photoDirectory = new DirectoryInfo(Path.Combine(settings.LocalSiteRootDirectory, "Posts"));
            if (!photoDirectory.Exists) photoDirectory.Create();

            photoDirectory.Refresh();

            return photoDirectory;
        }

        public static FileInfo LocalSitePostHtmlFile(this UserSettings settings, PostContent content)
        {
            var directory = settings.LocalSitePostContentDirectory(content, false);
            return new FileInfo($"{Path.Combine(directory.FullName, content.Slug)}.html");
        }

        public static string PhotoPageUrl(this UserSettings settings, PhotoContent content)
        {
            return $"//{settings.SiteUrl}/Photos/{content.Folder}/{content.Slug}/{content.Slug}.html";
        }

        public static string PhotoListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Photos/PhotoList.html";
        }

        public static string FileListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Files/FileList.html";
        }

        public static string ImageListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Images/ImageList.html";
        }

        public static string PostsListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/Posts/PostList.html";
        }

        public static string AllContentListUrl(this UserSettings settings)
        {
            return $"//{settings.SiteUrl}/AllContentList.html";
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

            return string.Empty;
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
            return new FileInfo(Path.Combine(StorageDirectory().FullName, "PointlessWaymarksCmsSettings.json"));
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