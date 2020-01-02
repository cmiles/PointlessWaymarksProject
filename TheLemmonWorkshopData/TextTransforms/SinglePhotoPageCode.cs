using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TheLemmonWorkshopData.Models;

namespace TheLemmonWorkshopData.TextTransforms
{
    public partial class SinglePhotoPage
    {
        public PhotoContent DbEntry { get; set; }
        public string SiteUrl { get; set; }
        public string SiteName { get; set; }
        public string PageUrl { get; set; }

        public ImageFileInformation DisplayImage { get; set; }

        public ImageFileInformation LargeImage { get; set; }
        public List<ImageFileInformation> SrcsetImages { get; set; }

        public string SrcSetString()
        {
            return string.Join(", ",
                SrcsetImages.OrderByDescending(x => x.Width).Select(x => $"{x.FileName} {x.Width}w"));
        }

        public void ProcessPhotosInDirectory(DirectoryInfo directoryInfo)
        {
            var baseFileNameList = DbEntry.OriginalFileName.Split(".").ToList();
            var baseFileName = string.Join("", baseFileNameList.Take(baseFileNameList.Count - 1));

            var fileVariants = directoryInfo.GetFiles().Where(x => x.Name.StartsWith($"{baseFileName}--")).ToList();

            var displayImageFile = fileVariants.Single(x => x.Name.Contains("--For-Display"));
            DisplayImage = new ImageFileInformation
            {
                FileName = displayImageFile.Name,
                Height =
                    int.Parse(Regex.Match(displayImageFile.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                        .Groups["height"].Value),
                Width = int.Parse(Regex
                    .Match(displayImageFile.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline).Groups["width"]
                    .Value),
                SiteUrl = $@"//{SiteUrl}/Photos/{DbEntry.Folder}/{DbEntry.Slug}/{displayImageFile.Name}"
            };

            var srcsetImageFiles = fileVariants.Where(x => x.Name.Contains("--Sized"));
            SrcsetImages = srcsetImageFiles.Select(x => new ImageFileInformation
            {
                FileName = x.Name,
                Height =
                    int.Parse(Regex.Match(x.Name, @".*--(?<height>\d*)h.*", RegexOptions.Singleline)
                        .Groups["height"].Value),
                Width = int.Parse(Regex.Match(x.Name, @".*--(?<width>\d*)w.*", RegexOptions.Singleline)
                    .Groups["width"].Value),
                SiteUrl = $@"//{SiteUrl}/Photos/{DbEntry.Folder}/{DbEntry.Slug}/{x.Name}"
            }).ToList();

            LargeImage = SrcsetImages.OrderByDescending(x => Math.Max(x.Height, x.Width)).First();
        }
    }
}