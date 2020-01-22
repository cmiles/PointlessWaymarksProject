using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class PictureAssetProcessing
    {
        public static PictureAssetInformation ProcessImageDirectory(Guid photoOrImageContentId)
        {
            var db = Db.Context().Result;

            var content = db.ImageContents.SingleOrDefault(x => x.ContentId == photoOrImageContentId);

            var settings = UserSettingsSingleton.CurrentSettings();

            var contentDirectory = settings.LocalSiteImageContentDirectory(content);

            return ProcessImageDirectory(content, contentDirectory, settings.SiteUrl);
        }

        public static PictureAssetInformation ProcessImageDirectory(ImageContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var contentDirectory = settings.LocalSiteImageContentDirectory(dbEntry);

            return ProcessImageDirectory(dbEntry, contentDirectory, settings.SiteUrl);
        }

        public static PictureAssetInformation ProcessImageDirectory(ImageContent dbEntry, DirectoryInfo directoryInfo,
            string siteUrl)
        {
            var toReturn = new PictureAssetInformation {DbEntry = dbEntry};

            var baseFileNameList = dbEntry.OriginalFileName.Split(".").ToList();
            var baseFileName = string.Join("", baseFileNameList.Take(baseFileNameList.Count - 1));

            var fileVariants = directoryInfo.GetFiles().Where(x => x.Name.StartsWith($"{baseFileName}--")).ToList();

            var displayImageFile = fileVariants.SingleOrDefault(x => x.Name.Contains("--For-Display"));

            if (displayImageFile != null && displayImageFile.Exists)
                toReturn.DisplayPicture = new PictureFileInformation
                {
                    FileName = displayImageFile.Name,
                    SiteUrl = $@"//{siteUrl}/Images/{dbEntry.Folder}/{dbEntry.Slug}/{displayImageFile.Name}",
                    File = displayImageFile,
                    AltText = dbEntry.AltText ?? string.Empty,
                    Height =
                        int.Parse(Regex
                            .Match(displayImageFile.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                            .Groups["height"].Value),
                    Width = int.Parse(Regex
                        .Match(displayImageFile.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                        .Groups["width"].Value)
                };

            var srcsetImageFiles = fileVariants.Where(x => x.Name.Contains("--Sized"));
            toReturn.SrcsetImages = srcsetImageFiles.Select(x => new PictureFileInformation
            {
                FileName = x.Name,
                Height =
                    int.Parse(Regex.Match(x.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                        .Groups["height"].Value),
                Width = int.Parse(Regex.Match(x.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                    .Groups["width"].Value),
                SiteUrl = $@"//{siteUrl}/Images/{dbEntry.Folder}/{dbEntry.Slug}/{x.Name}",
                AltText = dbEntry.AltText ?? string.Empty,
                File = x
            }).ToList();

            toReturn.LargePicture = toReturn.SrcsetImages.OrderByDescending(x => Math.Max(x.Height, x.Width)).First();
            toReturn.SmallPicture = toReturn.SrcsetImages.OrderBy(x => Math.Max(x.Height, x.Width)).First();

            return toReturn;
        }

        public static PictureAssetInformation ProcessPhotoDirectory(Guid photoOrImageContentId)
        {
            var db = Db.Context().Result;

            var content = db.PhotoContents.SingleOrDefault(x => x.ContentId == photoOrImageContentId);

            var settings = UserSettingsSingleton.CurrentSettings();

            var contentDirectory = settings.LocalSitePhotoContentDirectory(content);

            return ProcessPhotoDirectory(content, contentDirectory, settings.SiteUrl);
        }

        public static PictureAssetInformation ProcessPhotoDirectory(PhotoContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            var contentDirectory = settings.LocalSitePhotoContentDirectory(dbEntry);

            return ProcessPhotoDirectory(dbEntry, contentDirectory, settings.SiteUrl);
        }

        public static PictureAssetInformation ProcessPhotoDirectory(PhotoContent dbEntry, DirectoryInfo directoryInfo,
            string siteUrl)
        {
            var toReturn = new PictureAssetInformation {DbEntry = dbEntry};

            var baseFileNameList = dbEntry.OriginalFileName.Split(".").ToList();
            var baseFileName = string.Join("", baseFileNameList.Take(baseFileNameList.Count - 1));

            var fileVariants = directoryInfo.GetFiles().Where(x => x.Name.StartsWith($"{baseFileName}--")).ToList();

            var displayImageFile = fileVariants.SingleOrDefault(x => x.Name.Contains("--For-Display"));

            if (displayImageFile != null && displayImageFile.Exists)
                toReturn.DisplayPicture = new PictureFileInformation
                {
                    FileName = displayImageFile.Name,
                    SiteUrl = $@"//{siteUrl}/Photos/{dbEntry.Folder}/{dbEntry.Slug}/{displayImageFile.Name}",
                    File = displayImageFile,
                    AltText = dbEntry.AltText ?? string.Empty,
                    Height =
                        int.Parse(Regex
                            .Match(displayImageFile.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                            .Groups["height"].Value),
                    Width = int.Parse(Regex
                        .Match(displayImageFile.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                        .Groups["width"].Value)
                };

            var srcsetImageFiles = fileVariants.Where(x => x.Name.Contains("--Sized"));
            toReturn.SrcsetImages = srcsetImageFiles.Select(x => new PictureFileInformation
            {
                FileName = x.Name,
                SiteUrl = $@"//{siteUrl}/Photos/{dbEntry.Folder}/{dbEntry.Slug}/{x.Name}",
                File = x,
                AltText = dbEntry.AltText ?? string.Empty,
                Height =
                    int.Parse(Regex.Match(x.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                        .Groups["height"].Value),
                Width = int.Parse(Regex.Match(x.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                    .Groups["width"].Value)
            }).ToList();

            toReturn.LargePicture = toReturn.SrcsetImages.OrderByDescending(x => Math.Max(x.Height, x.Width)).First();
            toReturn.SmallPicture = toReturn.SrcsetImages.OrderBy(x => Math.Max(x.Height, x.Width)).First();

            return toReturn;
        }

        public static PictureAssetInformation ProcessPictureDirectory(Guid photoOrImageContentId)
        {
            var db = Db.Context().Result;

            var isPhoto = db.PhotoContents.Any(x => x.ContentId == photoOrImageContentId);

            return isPhoto
                ? ProcessPhotoDirectory(photoOrImageContentId)
                : ProcessImageDirectory(photoOrImageContentId);
        }
    }
}