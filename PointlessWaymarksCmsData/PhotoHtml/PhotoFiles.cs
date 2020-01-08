using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.PhotoHtml
{
    public static class PhotoFiles
    {
        public static PhotoDirectoryContentsInformation ProcessPhotosInDirectory(PhotoContent dbEntry)
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;
            var contentDirectory = settings.LocalSitePhotoContentDirectory(dbEntry);

            return ProcessPhotosInDirectory(dbEntry, contentDirectory, settings.SiteUrl);
        }

        public static PhotoDirectoryContentsInformation ProcessPhotosInDirectory(PhotoContent dbEntry,
            DirectoryInfo directoryInfo, string siteUrl)
        {
            var toReturn = new PhotoDirectoryContentsInformation();

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