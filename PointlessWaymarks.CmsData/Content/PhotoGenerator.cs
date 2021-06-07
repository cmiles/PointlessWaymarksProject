using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Xmp;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using XmpCore;

namespace PointlessWaymarks.CmsData.Content
{
    public static class PhotoGenerator
    {
        public static async Task GenerateHtml(PhotoContent toGenerate, DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            progress?.Report($"Photo Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SinglePhotoPage(toGenerate) {GenerationVersion = generationVersion};

            await htmlContext.WriteLocalHtml();
        }

        public static (GenerationReturn generationReturn, PhotoMetadata? metadata) PhotoMetadataFromFile(
            FileInfo selectedFile, IProgress<string>? progress = null)
        {
            progress?.Report("Starting Metadata Processing");

            selectedFile.Refresh();

            if (!selectedFile.Exists) return (GenerationReturn.Error("File Does Not Exist?"), null);

            var toReturn = new PhotoMetadata();

            progress?.Report("Getting Directories");

            var exifSubIfDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName)
                .OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var exifDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<ExifIfd0Directory>()
                .FirstOrDefault();
            var iptcDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<IptcDirectory>()
                .FirstOrDefault();
            var gpsDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<GpsDirectory>()
                .FirstOrDefault();
            var xmpDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<XmpDirectory>()
                .FirstOrDefault();

            toReturn.PhotoCreatedBy = exifDirectory?.GetDescription(ExifDirectoryBase.TagArtist) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(toReturn.PhotoCreatedBy))
                toReturn.PhotoCreatedBy = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "creator", 1)?.Value ??
                                          string.Empty;

            if (string.IsNullOrWhiteSpace(toReturn.PhotoCreatedBy))
                toReturn.PhotoCreatedBy = iptcDirectory?.GetDescription(IptcDirectory.TagByLine) ?? string.Empty;

            var createdOn = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
            if (string.IsNullOrWhiteSpace(createdOn))
            {
                var gpsDateTime = DateTime.MinValue;
                if (gpsDirectory?.TryGetGpsDate(out gpsDateTime) ?? false)
                {
                    if (gpsDateTime != DateTime.MinValue) toReturn.PhotoCreatedOn = gpsDateTime.ToLocalTime();
                }
                else
                {
                    toReturn.PhotoCreatedOn = DateTime.Now;
                }
            }
            else
            {
                var createdOnParsed = DateTime.TryParseExact(
                    exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal), "yyyy:MM:dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate);

                toReturn.PhotoCreatedOn = createdOnParsed ? parsedDate : DateTime.Now;
            }

            var isoString = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagIsoEquivalent);
            if (!string.IsNullOrWhiteSpace(isoString)) toReturn.Iso = int.Parse(isoString);

            toReturn.CameraMake = exifDirectory?.GetDescription(ExifDirectoryBase.TagMake) ?? string.Empty;
            toReturn.CameraModel = exifDirectory?.GetDescription(ExifDirectoryBase.TagModel) ?? string.Empty;
            toReturn.FocalLength = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength) ?? string.Empty;

            toReturn.Lens = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagLensModel) ?? string.Empty;

            if (toReturn.Lens == string.Empty || toReturn.Lens == "----")
                toReturn.Lens = xmpDirectory?.XmpMeta?.GetProperty(XmpConstants.NsExifAux, "Lens")?.Value ??
                                string.Empty;
            if (toReturn.Lens == string.Empty || toReturn.Lens == "----")
            {
                toReturn.Lens =
                    xmpDirectory?.XmpMeta?.GetProperty(XmpConstants.NsCameraraw, "LensProfileName")?.Value ??
                    string.Empty;

                if (toReturn.Lens.StartsWith("Adobe ("))
                {
                    toReturn.Lens = toReturn.Lens[7..];
                    if (toReturn.Lens.EndsWith(")"))
                        toReturn.Lens = toReturn.Lens[..^1];
                }
            }

            if (toReturn.Lens == "----") toReturn.Lens = string.Empty;

            toReturn.Aperture = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagAperture) ?? string.Empty;

            toReturn.License = exifDirectory?.GetDescription(ExifDirectoryBase.TagCopyright) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(toReturn.License))
                toReturn.License = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "rights", 1)?.Value ??
                                   string.Empty;

            if (string.IsNullOrWhiteSpace(toReturn.License))
                toReturn.License = iptcDirectory?.GetDescription(IptcDirectory.TagCopyrightNotice) ?? string.Empty;

            var shutterValue = new Rational();
            if (exifSubIfDirectory?.TryGetRational(37377, out shutterValue) ?? false)
                toReturn.ShutterSpeed = ExifHelpers.ShutterSpeedToHumanReadableString(shutterValue);
            else
                toReturn.ShutterSpeed = string.Empty;

            //The XMP data - vs the IPTC - will hold the full Title for a very long title (the IPTC will be truncated) -
            //for a 'from Lightroom with no other concerns' export Title makes the most sense, but there are other possible
            //metadata fields to pull from that could be relevant in other contexts.
            toReturn.Title = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "title", 1)?.Value;

            if (string.IsNullOrWhiteSpace(toReturn.Title))
                toReturn.Title = iptcDirectory?.GetDescription(IptcDirectory.TagObjectName) ?? string.Empty;
            //Use a variety of guess on common file names and make that the title - while this could result in an initial title
            //like DSC001 style out of camera names but after having experimented with loading files I think 'default' is better
            //than an invalid blank.
            if (string.IsNullOrWhiteSpace(toReturn.Title))
                toReturn.Title = Path.GetFileNameWithoutExtension(selectedFile.Name).Replace("-", " ").Replace("_", " ")
                    .SplitCamelCase();

            toReturn.Summary = iptcDirectory?.GetDescription(IptcDirectory.TagObjectName) ?? string.Empty;

            //2020/3/22 - Process out a convention that I have used for more than a decade of pre-2020s do yyMM at the start of a photo title or in the
            //2020s yyyy MM at the start - hopefully this is matched specifically enough not to accidentally trigger on photos without this info... The
            //base case of not matching this convention is that the year and month from photo created on are added to the front of the title or if the
            //title is blank the photo created on is put as a timestamp into the title. One of the ideas here is to give photos as much of a chance
            //as possible to have valid data for an automated import even when a detail like timestamp title is probably better replaced with something
            //more descriptive.
            if (!string.IsNullOrWhiteSpace(toReturn.Title) && toReturn.Title.StartsWith("2"))
            {
                var possibleTitleDate =
                    Regex.Match(toReturn.Title, @"\A(?<possibleDate>\d\d\d\d[\s-]\d\d[\s-]*).*",
                        RegexOptions.IgnoreCase).Groups["possibleDate"].Value;
                if (!string.IsNullOrWhiteSpace(possibleTitleDate))
                    try
                    {
                        var tempDate = new DateTime(int.Parse(possibleTitleDate.Substring(0, 4)),
                            int.Parse(possibleTitleDate.Substring(5, 2)), 1);

                        toReturn.Summary = $"{toReturn.Title[possibleTitleDate.Length..]}";
                        toReturn.Title =
                            $"{tempDate:yyyy} {tempDate:MMMM} {toReturn.Title[possibleTitleDate.Length..]}";

                        progress?.Report("Title updated based on 2yyy MM start pattern for file name");
                    }
                    catch
                    {
                        progress?.Report("Did not successfully parse 2yyy MM start pattern for file name");
                    }
            }
            else if (!string.IsNullOrWhiteSpace(toReturn.Title) &&
                     (toReturn.Title.StartsWith("0") || toReturn.Title.StartsWith("1")))
            {
                try
                {
                    if (Regex.IsMatch(toReturn.Title, @"\A[01]\d\d\d\s.*", RegexOptions.IgnoreCase))
                    {
                        var year = int.Parse(toReturn.Title.Substring(0, 2));
                        var month = int.Parse(toReturn.Title.Substring(2, 2));

                        var tempDate = year < 20
                            ? new DateTime(2000 + year, month, 1)
                            : new DateTime(1900 + year, month, 1);

                        toReturn.Summary = $"{toReturn.Title[5..]}";
                        toReturn.Title = $"{tempDate:yyyy} {tempDate:MMMM} {toReturn.Title[5..]}";

                        progress?.Report("Title updated based on YYMM start pattern for file name");
                    }
                }
                catch
                {
                    progress?.Report("Did not successfully parse YYMM start pattern for file name");
                }
            }
            else if (Regex.IsMatch(toReturn.Title, @".*[\s-][01]\d\d\d\z", RegexOptions.IgnoreCase))
            {
                try
                {
                    var year = int.Parse(toReturn.Title.Substring(toReturn.Title.Length - 4, 2));
                    var month = int.Parse(toReturn.Title.Substring(toReturn.Title.Length - 2, 2));

                    var tempDate = year < 20
                        ? new DateTime(2000 + year, month, 1)
                        : new DateTime(1900 + year, month, 1);

                    toReturn.Summary = $"{toReturn.Title[..^5]}";
                    toReturn.Title = $"{tempDate:yyyy} {tempDate:MMMM} {toReturn.Title[..^5]}";

                    progress?.Report("Title updated based on YYMM end pattern for file name");
                }
                catch
                {
                    progress?.Report("Did not successfully parse YYMM end pattern for file name");
                }
            }
            else if (Regex.IsMatch(toReturn.Title, @".*[\s-]\d\d\d\d[\s-]\d\d\z", RegexOptions.IgnoreCase))
            {
                var possibleTitleDate =
                    Regex.Match(toReturn.Title, @".*[\s-](?<possibleDate>\d\d\d\d[\s-]\d\d)\z", RegexOptions.IgnoreCase)
                        .Groups["possibleDate"].Value;
                if (!string.IsNullOrWhiteSpace(possibleTitleDate))
                    try
                    {
                        var tempDate = new DateTime(int.Parse(possibleTitleDate.Substring(0, 4)),
                            int.Parse(possibleTitleDate.Substring(5, 2)), 1);

                        toReturn.Summary =
                            $"{toReturn.Title.Substring(0, toReturn.Title.Length - possibleTitleDate.Length)}";
                        toReturn.Title =
                            $"{tempDate:yyyy} {tempDate:MMMM} {toReturn.Title.Substring(0, toReturn.Title.Length - possibleTitleDate.Length)}";

                        progress?.Report("Title updated based on 2yyy MM end pattern for file name");
                    }
                    catch
                    {
                        progress?.Report("Did not successfully parse 2yyy MM end pattern for file name");
                    }
            }
            else
            {
                toReturn.Title = string.IsNullOrWhiteSpace(toReturn.Title)
                    ? toReturn.PhotoCreatedOn.ToString("yyyy MMMM dd h-mm-ss tt")
                    : $"{toReturn.PhotoCreatedOn:yyyy} {toReturn.PhotoCreatedOn:MMMM} {toReturn.Title}";
            }

            //Order is important here - the title supplies the summary in the code above - but overwrite that if there is a
            //description.
            var description = exifDirectory?.GetDescription(ExifDirectoryBase.TagImageDescription) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(description))
                toReturn.Summary = description;

            //Add a trailing . to the summary if it doesn't end with ! ? .
            if (!toReturn.Summary.EndsWith(".") && !toReturn.Summary.EndsWith("!") && !toReturn.Summary.EndsWith("?"))
                toReturn.Summary = $"{toReturn.Summary}.";

            //Remove multi space from title and summary
            if (!string.IsNullOrWhiteSpace(toReturn.Title))
                toReturn.Title = Regex.Replace(toReturn.Title, @"\s+", " ").TrimNullToEmpty();

            if (!string.IsNullOrWhiteSpace(toReturn.Summary))
                toReturn.Summary = Regex.Replace(toReturn.Summary, @"\s+", " ").TrimNullToEmpty();

            var xmpSubjectKeywordList = new List<string>();

            var xmpSubjectArrayItemCount = xmpDirectory?.XmpMeta?.CountArrayItems(XmpConstants.NsDC, "subject");

            if (xmpSubjectArrayItemCount != null)
                for (var i = 1; i <= xmpSubjectArrayItemCount; i++)
                {
                    var subjectArrayItem = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "subject", i);
                    if (subjectArrayItem == null || string.IsNullOrWhiteSpace(subjectArrayItem.Value)) continue;
                    xmpSubjectKeywordList.AddRange(subjectArrayItem.Value.Replace(";", ",").Split(",")
                        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                }

            xmpSubjectKeywordList = xmpSubjectKeywordList.Distinct().ToList();

            var keywordTagList = new List<string>();

            var keywordValue = iptcDirectory?.GetDescription(IptcDirectory.TagKeywords)?.Replace(";", ",") ??
                               string.Empty;

            if (!string.IsNullOrWhiteSpace(keywordValue))
                keywordTagList.AddRange(keywordValue.Replace(";", ",").Split(",")
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

            if (xmpSubjectKeywordList.Count == 0 && keywordTagList.Count == 0)
                toReturn.Tags = string.Empty;
            else if (xmpSubjectKeywordList.Count >= keywordTagList.Count)
                toReturn.Tags = string.Join(",", xmpSubjectKeywordList);
            else
                toReturn.Tags = string.Join(",", keywordTagList);

            if (!string.IsNullOrWhiteSpace(toReturn.Tags))
                toReturn.Tags = Db.TagListJoin(Db.TagListParse(toReturn.Tags));

            return (GenerationReturn.Success($"Parsed Photo Metadata for {selectedFile.FullName} without error"),
                toReturn);
        }

        public static (GenerationReturn, PhotoContent?) PhotoMetadataToNewPhotoContent(FileInfo selectedFile,
            IProgress<string> progress, string? photoContentCreatedBy = null)
        {
            selectedFile.Refresh();

            if (!selectedFile.Exists) return (GenerationReturn.Error("File Does Not Exist?"), null);

            var (generationReturn, metadata) = PhotoMetadataFromFile(selectedFile, progress);

            if (generationReturn.HasError) return (generationReturn, null);

            var toReturn = new PhotoContent();

            toReturn.InjectFrom(metadata);

            toReturn.OriginalFileName = selectedFile.Name;
            toReturn.ContentId = Guid.NewGuid();
            toReturn.CreatedBy = string.IsNullOrWhiteSpace(photoContentCreatedBy)
                ? UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
                : photoContentCreatedBy.Trim();
            toReturn.CreatedOn = DateTime.Now;
            toReturn.Slug = SlugUtility.Create(true, toReturn.Title);
            toReturn.BodyContentFormat = ContentFormatDefaults.Content.ToString();
            toReturn.UpdateNotesFormat = ContentFormatDefaults.Content.ToString();

            var possibleTitleYear = Regex.Match(toReturn.Title ?? string.Empty,
                @"\A(?<possibleYear>\d\d\d\d) (?<possibleMonth>January?|February?|March?|April?|May|June?|July?|August?|September?|October?|November?|December?) .*",
                RegexOptions.IgnoreCase).Groups["possibleYear"].Value;
            if (!string.IsNullOrWhiteSpace(possibleTitleYear))
                if (int.TryParse(possibleTitleYear, out var convertedYear))
                    if (convertedYear >= 1826 && convertedYear <= DateTime.Now.Year)
                        toReturn.Folder = convertedYear.ToString("F0");

            if (string.IsNullOrWhiteSpace(toReturn.Folder))
                toReturn.Folder = toReturn.PhotoCreatedOn.Year.ToString("F0");

            return (GenerationReturn.Success($"Parsed Photo Metadata for {selectedFile.FullName} without error"),
                toReturn);
        }


        public static async Task<(GenerationReturn generationReturn, PhotoContent? photoContent)> SaveAndGenerateHtml(
            PhotoContent toSave, FileInfo selectedFile, bool overwriteExistingFiles, DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            var validationReturn = await Validate(toSave, selectedFile);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await FileManagement.WriteSelectedPhotoContentFileToMediaArchive(selectedFile);
            await Db.SavePhotoContent(toSave);
            await WritePhotoFromMediaArchiveToLocalSite(toSave, overwriteExistingFiles, progress);
            await GenerateHtml(toSave, generationVersion, progress);
            await Export.WriteLocalDbJson(toSave);

            DataNotifications.PublishDataNotification("Photo Generator", DataNotificationContentType.Photo,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<(GenerationReturn generationReturn, PhotoContent? photoContent)> SaveToDb(
            PhotoContent toSave, FileInfo selectedFile)
        {
            var validationReturn = await Validate(toSave, selectedFile);

            if (validationReturn.HasError) return (validationReturn, null);

            await FileManagement.WriteSelectedPhotoContentFileToMediaArchive(selectedFile);
            await Db.SavePhotoContent(toSave);

            return (GenerationReturn.Success($"Saved {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(PhotoContent? photoContent, FileInfo? selectedFile)
        {
            if (photoContent == null)
                return GenerationReturn.Error("Null Photo Content submitted to Validate?");

            if (selectedFile == null)
                return GenerationReturn.Error("No Photo File submitted to Validate?", photoContent.ContentId);

            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Valid)
                return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                    photoContent.ContentId);

            var mediaArchiveCheck = UserSettingsUtilities.ValidateLocalMediaArchive();
            if (!mediaArchiveCheck.Valid)
                return GenerationReturn.Error($"Problem with Media Archive: {mediaArchiveCheck.Explanation}",
                    photoContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(photoContent);
            if (!commonContentCheck.Valid)
                return GenerationReturn.Error(commonContentCheck.Explanation, photoContent.ContentId);

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(photoContent.UpdateNotesFormat);
            if (!updateFormatCheck.Valid)
                return GenerationReturn.Error(updateFormatCheck.Explanation, photoContent.ContentId);

            selectedFile.Refresh();

            var photoFileValidation =
                await CommonContentValidation.PhotoFileValidation(selectedFile, photoContent.ContentId);

            if (!photoFileValidation.Valid)
                return GenerationReturn.Error(photoFileValidation.Explanation, photoContent.ContentId);

            return GenerationReturn.Success("Photo Content Validation Successful");
        }

        public static async Task WritePhotoFromMediaArchiveToLocalSite(PhotoContent photoContent,
            bool forcedResizeOverwriteExistingFiles, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(photoContent.OriginalFileName)) return;

            var userSettings = UserSettingsSingleton.CurrentSettings();

            var sourceFile = new FileInfo(Path.Combine(userSettings.LocalMediaArchivePhotoDirectory().FullName,
                photoContent.OriginalFileName));

            var targetFile = new FileInfo(Path.Combine(
                userSettings.LocalSitePhotoContentDirectory(photoContent).FullName, photoContent.OriginalFileName));

            if (targetFile.Exists && forcedResizeOverwriteExistingFiles)
            {
                targetFile.Delete();
                targetFile.Refresh();
            }

            if (!targetFile.Exists) await sourceFile.CopyToAndLogAsync(targetFile.FullName);

            PictureResizing.DeleteSupportedPictureFilesInDirectoryOtherThanOriginalFile(photoContent, progress);

            PictureResizing.CleanDisplayAndSrcSetFilesInPhotoDirectory(photoContent, forcedResizeOverwriteExistingFiles,
                progress);

            await PictureResizing.ResizeForDisplayAndSrcset(photoContent, forcedResizeOverwriteExistingFiles, progress);
        }
    }
}