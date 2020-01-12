using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.PhotoHtml
{
    public static class PhotoAndImageFiles
    {
        public static ImageDirectoryContentsInformation ProcessImageDirectory(Guid photoOrImageContentId)
        {
            var db = Db.Context().Result;

            var content = db.ImageContents.SingleOrDefault(x => x.ContentId == photoOrImageContentId);

            if (content == null) content = db.ImageContents.SingleOrDefault(x => x.ContentId == photoOrImageContentId);
            ;

            var settings = UserSettingsUtilities.ReadSettings().Result;

            var contentDirectory = settings.LocalSiteImageContentDirectory(content);

            return ProcessImageDirectory(content, contentDirectory, settings.SiteUrl);
        }

        public static ImageDirectoryContentsInformation ProcessImageDirectory(ImageContent dbEntry)
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;
            var contentDirectory = settings.LocalSiteImageContentDirectory(dbEntry);

            return ProcessImageDirectory(dbEntry, contentDirectory, settings.SiteUrl);
        }

        public static ImageDirectoryContentsInformation ProcessImageDirectory(ImageContent dbEntry,
            DirectoryInfo directoryInfo, string siteUrl)
        {
            var toReturn = new ImageDirectoryContentsInformation();

            var baseFileNameList = dbEntry.OriginalFileName.Split(".").ToList();
            var baseFileName = string.Join("", baseFileNameList.Take(baseFileNameList.Count - 1));

            var fileVariants = directoryInfo.GetFiles().Where(x => x.Name.StartsWith($"{baseFileName}--")).ToList();

            var displayImageFile = fileVariants.SingleOrDefault(x => x.Name.Contains("--For-Display"));

            if (displayImageFile != null && displayImageFile.Exists)
                toReturn.DisplayImage = new ImageFileInformation
                {
                    FileName = displayImageFile.Name,
                    SiteUrl = $@"//{siteUrl}/Images/{dbEntry.Folder}/{dbEntry.Slug}/{displayImageFile.Name}",
                    File = displayImageFile,
                    Height =
                        int.Parse(Regex
                            .Match(displayImageFile.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                            .Groups["height"].Value),
                    Width = int.Parse(Regex
                        .Match(displayImageFile.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                        .Groups["width"].Value)
                };

            var srcsetImageFiles = fileVariants.Where(x => x.Name.Contains("--Sized"));
            toReturn.SrcsetImages = srcsetImageFiles.Select(x => new ImageFileInformation
            {
                FileName = x.Name,
                Height =
                    int.Parse(Regex.Match(x.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                        .Groups["height"].Value),
                Width = int.Parse(Regex.Match(x.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                    .Groups["width"].Value),
                SiteUrl = $@"//{siteUrl}/Images/{dbEntry.Folder}/{dbEntry.Slug}/{x.Name}",
                File = x
            }).ToList();

            toReturn.LargeImage = toReturn.SrcsetImages.OrderByDescending(x => Math.Max(x.Height, x.Width)).First();
            toReturn.SmallImage = toReturn.SrcsetImages.OrderBy(x => Math.Max(x.Height, x.Width)).First();

            return toReturn;
        }

        public static ImageDirectoryContentsInformation ProcessImageOrPhotoDirectory(Guid photoOrImageContentId)
        {
            var db = Db.Context().Result;

            var isPhoto = db.PhotoContents.Any(x => x.ContentId == photoOrImageContentId);

            return isPhoto
                ? ProcessPhotoDirectory(photoOrImageContentId)
                : ProcessImageDirectory(photoOrImageContentId);
        }

        public static ImageDirectoryContentsInformation ProcessPhotoDirectory(Guid photoOrImageContentId)
        {
            var db = Db.Context().Result;

            var content = db.PhotoContents.SingleOrDefault(x => x.ContentId == photoOrImageContentId);

            if (content == null) content = db.PhotoContents.SingleOrDefault(x => x.ContentId == photoOrImageContentId);
            ;

            var settings = UserSettingsUtilities.ReadSettings().Result;

            var contentDirectory = settings.LocalSitePhotoContentDirectory(content);

            return ProcessPhotoDirectory(content, contentDirectory, settings.SiteUrl);
        }

        public static ImageDirectoryContentsInformation ProcessPhotoDirectory(PhotoContent dbEntry)
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;
            var contentDirectory = settings.LocalSitePhotoContentDirectory(dbEntry);

            return ProcessPhotoDirectory(dbEntry, contentDirectory, settings.SiteUrl);
        }

        public static ImageDirectoryContentsInformation ProcessPhotoDirectory(PhotoContent dbEntry,
            DirectoryInfo directoryInfo, string siteUrl)
        {
            var toReturn = new ImageDirectoryContentsInformation();

            var baseFileNameList = dbEntry.OriginalFileName.Split(".").ToList();
            var baseFileName = string.Join("", baseFileNameList.Take(baseFileNameList.Count - 1));

            var fileVariants = directoryInfo.GetFiles().Where(x => x.Name.StartsWith($"{baseFileName}--")).ToList();

            var displayImageFile = fileVariants.SingleOrDefault(x => x.Name.Contains("--For-Display"));

            if (displayImageFile != null && displayImageFile.Exists)
                toReturn.DisplayImage = new ImageFileInformation
                {
                    FileName = displayImageFile.Name,
                    SiteUrl = $@"//{siteUrl}/Photos/{dbEntry.Folder}/{dbEntry.Slug}/{displayImageFile.Name}",
                    File = displayImageFile,
                    Height =
                        int.Parse(Regex
                            .Match(displayImageFile.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                            .Groups["height"].Value),
                    Width = int.Parse(Regex
                        .Match(displayImageFile.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                        .Groups["width"].Value)
                };

            var srcsetImageFiles = fileVariants.Where(x => x.Name.Contains("--Sized"));
            toReturn.SrcsetImages = srcsetImageFiles.Select(x => new ImageFileInformation
            {
                FileName = x.Name,
                Height =
                    int.Parse(Regex.Match(x.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                        .Groups["height"].Value),
                Width = int.Parse(Regex.Match(x.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                    .Groups["width"].Value),
                SiteUrl = $@"//{siteUrl}/Photos/{dbEntry.Folder}/{dbEntry.Slug}/{x.Name}",
                File = x
            }).ToList();

            toReturn.LargeImage = toReturn.SrcsetImages.OrderByDescending(x => Math.Max(x.Height, x.Width)).First();
            toReturn.SmallImage = toReturn.SrcsetImages.OrderBy(x => Math.Max(x.Height, x.Width)).First();

            return toReturn;
        }
    }
}