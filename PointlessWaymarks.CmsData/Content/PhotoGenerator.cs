using System.Globalization;
using System.Text.RegularExpressions;
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
using PointlessWaymarks.CmsData.Spatial.Elevation;
using XmpCore;

namespace PointlessWaymarks.CmsData.Content;

public static class PhotoGenerator
{
    public static async Task GenerateHtml(PhotoContent toGenerate, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Photo Content - Generate HTML for {toGenerate.Title}");

        var htmlContext = new SinglePhotoPage(toGenerate) { GenerationVersion = generationVersion };

        await htmlContext.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task<(GenerationReturn generationReturn, PhotoMetadata? metadata)> PhotoMetadataFromFile(
        FileInfo selectedFile, IProgress<string>? progress = null)
    {
        progress?.Report("Starting Metadata Processing");

        selectedFile.Refresh();

        if (!selectedFile.Exists) return (GenerationReturn.Error("File Does Not Exist?"), null);

        var toReturn = new PhotoMetadata();

        progress?.Report("Getting Directories");

        var exifSubIfDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<ExifSubIfdDirectory>()
            .FirstOrDefault();
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

        //Created on stack of choices - this is a very anecdotal list and ordering based on photos I have seen that 
        //are mostly mine - this could no doubt be improved with some research into what various open source 
        //photo/media imports are doing and/or finding a great test set of photos
        var createdOn = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
        if (string.IsNullOrWhiteSpace(createdOn))
            createdOn = exifDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
        if (string.IsNullOrWhiteSpace(createdOn))
            createdOn = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagDateTime);
        if (string.IsNullOrWhiteSpace(createdOn))
            createdOn = exifDirectory?.GetDescription(ExifDirectoryBase.TagDateTime);

        //If no string is returned from the EXIF then try for GPX information and fallback to now
        if (string.IsNullOrWhiteSpace(createdOn))
        {
            if (gpsDirectory?.TryGetGpsDate(out var gpsDateTime) ?? false)
            {
                if (gpsDateTime != DateTime.MinValue) toReturn.PhotoCreatedOn = gpsDateTime.ToLocalTime();
            }
            else
            {
                toReturn.PhotoCreatedOn = DateTime.Now;
            }
        }
        //If a string was found in the Exif try to parse it or fallback to now
        else
        {
            var createdOnParsed = DateTime.TryParseExact(createdOn, "yyyy:MM:dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate);

            toReturn.PhotoCreatedOn = createdOnParsed ? parsedDate : DateTime.Now;
        }

        if (gpsDirectory != null)
        {
            var geoLocation = gpsDirectory.GetGeoLocation();

            if (geoLocation?.IsZero ?? true)
            {
                toReturn.Longitude = null;
                toReturn.Latitude = null;
            }
            else
            {
                toReturn.Latitude = gpsDirectory.GetGeoLocation()?.Latitude;
                toReturn.Longitude = gpsDirectory.GetGeoLocation()?.Longitude;
            }

            if (toReturn.Latitude != null && toReturn.Longitude != null)
            {
                var foundAltitude = false;

                if (toReturn.Latitude != null && toReturn.Longitude != null)
                {
                    var hasSeaLevelIndicator =
                        gpsDirectory.TryGetByte(GpsDirectory.TagAltitudeRef, out var seaLevelIndicator);
                    var hasElevation = gpsDirectory.TryGetRational(GpsDirectory.TagAltitude, out var altitudeRational);

                    if (hasElevation)
                    {
                        var isBelowSeaLevel = false;

                        if (hasSeaLevelIndicator) isBelowSeaLevel = seaLevelIndicator == 1;

                        if (altitudeRational.Denominator != 0 ||
                            (altitudeRational.Denominator == 0 && altitudeRational.Numerator == 0))
                        {
                            toReturn.Elevation = isBelowSeaLevel
                                ? altitudeRational.ToDouble() * -1
                                : altitudeRational.ToDouble();
                            foundAltitude = true;
                        }
                    }

                    if (!foundAltitude)
                        try
                        {
                            toReturn.Elevation = await ElevationService.OpenTopoNedElevation(toReturn.Latitude.Value,
                                toReturn.Longitude.Value, progress);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                }
            }
        }


        var isoString = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagIsoEquivalent);
        if (!string.IsNullOrWhiteSpace(isoString)) toReturn.Iso = int.Parse(isoString);

        toReturn.CameraMake = exifDirectory?.GetDescription(ExifDirectoryBase.TagMake) ?? string.Empty;
        toReturn.CameraModel = exifDirectory?.GetDescription(ExifDirectoryBase.TagModel) ?? string.Empty;
        toReturn.FocalLength = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength) ?? string.Empty;

        toReturn.Lens = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagLensModel) ?? string.Empty;

        if (toReturn.Lens is "" or "----")
            toReturn.Lens = xmpDirectory?.XmpMeta?.GetProperty(XmpConstants.NsExifAux, "Lens")?.Value ?? string.Empty;
        if (toReturn.Lens is "" or "----")
        {
            toReturn.Lens = xmpDirectory?.XmpMeta?.GetProperty(XmpConstants.NsCameraraw, "LensProfileName")?.Value ??
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

        // ReSharper disable once InlineOutVariableDeclaration - Better to establish type of shutterValue explicitly
        Rational shutterValue;
        if (exifSubIfDirectory?.TryGetRational(ExifDirectoryBase.TagShutterSpeed, out shutterValue) ?? false)
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

        var fourDigitYearAndTwoDigitMonthAtStart =
            new Regex(@"\A(?<possibleDate>\d\d\d\d[\s-]\d\d[\s-]*).*", RegexOptions.IgnoreCase);
        var fourDigitYearAndTwoDigitMonthAtEnd =
            new Regex(@".*[\s-](?<possibleDate>\d\d\d\d[\s-]\d\d)\z", RegexOptions.IgnoreCase);
        var twoDigitYearAndTwoDigitMonthAtStart = new Regex(@"\A[01]\d\d\d\s.*", RegexOptions.IgnoreCase);
        var twoDigitYearAndTwoDigitMonthAtEnd = new Regex(@".*[\s-][01]\d\d\d\z", RegexOptions.IgnoreCase);

        if (!string.IsNullOrWhiteSpace(toReturn.Title) &&
            (toReturn.Title.StartsWith("1") || toReturn.Title.StartsWith("2")) &&
            fourDigitYearAndTwoDigitMonthAtStart.IsMatch(toReturn.Title))
        {
            var possibleTitleDate =
                fourDigitYearAndTwoDigitMonthAtStart.Match(toReturn.Title)
                    .Groups["possibleDate"].Value;
            if (!string.IsNullOrWhiteSpace(possibleTitleDate))
                try
                {
                    var tempDate = new DateTime(int.Parse(possibleTitleDate[..4]),
                        int.Parse(possibleTitleDate.Substring(5, 2)), 1);

                    toReturn.Summary = $"{toReturn.Title[possibleTitleDate.Length..]}".TrimNullToEmpty();
                    toReturn.Title =
                        $"{tempDate:yyyy} {tempDate:MMMM} {toReturn.Title[possibleTitleDate.Length..].TrimNullToEmpty()}";

                    progress?.Report("Title updated based on 2yyy MM start pattern for file name");
                }
                catch
                {
                    progress?.Report("Did not successfully parse 2yyy MM start pattern for file name");
                }
        }
        else if (!string.IsNullOrWhiteSpace(toReturn.Title) &&
                 (toReturn.Title.StartsWith("0") || toReturn.Title.StartsWith("1")) &&
                 twoDigitYearAndTwoDigitMonthAtStart.IsMatch(toReturn.Title))
        {
            try
            {
                var year = int.Parse(toReturn.Title[..2]);
                var month = int.Parse(toReturn.Title.Substring(2, 2));

                var tempDate = year < 20
                    ? new DateTime(2000 + year, month, 1)
                    : new DateTime(1900 + year, month, 1);

                toReturn.Summary = $"{toReturn.Title[5..]}".TrimNullToEmpty();
                toReturn.Title = $"{tempDate:yyyy} {tempDate:MMMM} {toReturn.Title[5..].TrimNullToEmpty()}";

                progress?.Report("Title updated based on YYMM start pattern for file name");
            }
            catch
            {
                progress?.Report("Did not successfully parse YYMM start pattern for file name");
            }
        }
        else if (twoDigitYearAndTwoDigitMonthAtEnd.IsMatch(toReturn.Title))
        {
            try
            {
                var year = int.Parse(toReturn.Title.Substring(toReturn.Title.Length - 4, 2));
                var month = int.Parse(toReturn.Title.Substring(toReturn.Title.Length - 2, 2));

                var tempDate = year < 20 ? new DateTime(2000 + year, month, 1) : new DateTime(1900 + year, month, 1);

                toReturn.Summary = $"{toReturn.Title[..^5]}".TrimNullToEmpty();
                toReturn.Title = $"{tempDate:yyyy} {tempDate:MMMM} {toReturn.Title[..^5].TrimNullToEmpty()}";

                progress?.Report("Title updated based on YYMM end pattern for file name");
            }
            catch
            {
                progress?.Report("Did not successfully parse YYMM end pattern for file name");
            }
        }
        else if (fourDigitYearAndTwoDigitMonthAtEnd.IsMatch(toReturn.Title))
        {
            var possibleTitleDate =
                fourDigitYearAndTwoDigitMonthAtEnd.Match(toReturn.Title)
                    .Groups["possibleDate"].Value;
            if (!string.IsNullOrWhiteSpace(possibleTitleDate))
                try
                {
                    var tempDate = new DateTime(int.Parse(possibleTitleDate[..4]),
                        int.Parse(possibleTitleDate.Substring(5, 2)), 1);

                    toReturn.Summary = $"{toReturn.Title[..^possibleTitleDate.Length].TrimNullToEmpty()}";
                    toReturn.Title =
                        $"{tempDate:yyyy} {tempDate:MMMM} {toReturn.Title[..^possibleTitleDate.Length].TrimNullToEmpty()}";

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
                : $"{toReturn.PhotoCreatedOn:yyyy} {toReturn.PhotoCreatedOn:MMMM} {toReturn.Title.TrimNullToEmpty()}";
        }

        if (string.IsNullOrWhiteSpace(toReturn.Summary)) toReturn.Summary = toReturn.Title;

        //Order is important here - the title supplies the summary in the code above - but overwrite that if there is a
        //description.
        var description = exifDirectory?.GetDescription(ExifDirectoryBase.TagImageDescription) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(description))
            toReturn.Summary = description;

        //Add a trailing . to the summary if it doesn't end with ! ? .
        if (!string.IsNullOrWhiteSpace(toReturn.Summary) && !toReturn.Summary.EndsWith(".") &&
            !toReturn.Summary.EndsWith("!") && !toReturn.Summary.EndsWith("?"))
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

        var keywordValue = iptcDirectory?.GetDescription(IptcDirectory.TagKeywords)?.Replace(";", ",") ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(keywordValue))
            keywordTagList.AddRange(keywordValue.Replace(";", ",").Split(",").Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()));

        if (xmpSubjectKeywordList.Count == 0 && keywordTagList.Count == 0)
            toReturn.Tags = string.Empty;
        else if (xmpSubjectKeywordList.Count >= keywordTagList.Count)
            toReturn.Tags = string.Join(",", xmpSubjectKeywordList);
        else
            toReturn.Tags = string.Join(",", keywordTagList);

        if (!string.IsNullOrWhiteSpace(toReturn.Tags))
            toReturn.Tags = Db.TagListJoin(Db.TagListParse(toReturn.Tags));

        return (GenerationReturn.Success($"Parsed Photo Metadata for {selectedFile.FullName} without error"), toReturn);
    }

    public static async Task<(GenerationReturn, PhotoContent?)> PhotoMetadataToNewPhotoContent(FileInfo selectedFile,
        IProgress<string> progress, string? photoContentCreatedBy = null)
    {
        selectedFile.Refresh();

        if (!selectedFile.Exists) return (GenerationReturn.Error("File Does Not Exist?"), null);

        var (generationReturn, metadata) = await PhotoMetadataFromFile(selectedFile, progress);

        if (generationReturn.HasError) return (generationReturn, null);

        var toReturn = new PhotoContent();

        toReturn.InjectFrom(metadata);

        var created = DateTime.Now;

        toReturn.OriginalFileName = selectedFile.Name;
        toReturn.ContentId = Guid.NewGuid();
        toReturn.CreatedBy = string.IsNullOrWhiteSpace(photoContentCreatedBy)
            ? UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
            : photoContentCreatedBy.Trim();
        toReturn.CreatedOn = created;
        toReturn.FeedOn = created;
        toReturn.Slug = SlugUtility.Create(true, toReturn.Title);
        toReturn.BodyContentFormat = ContentFormatDefaults.Content.ToString();
        toReturn.UpdateNotesFormat = ContentFormatDefaults.Content.ToString();
        toReturn.ShowPhotoSizes = UserSettingsSingleton.CurrentSettings().PhotoPagesHaveLinksToPhotoSizesByDefault;

        var possibleTitleYear = Regex.Match(toReturn.Title ?? string.Empty,
            @"\A(?<possibleYear>\d\d\d\d) (?<possibleMonth>January?|February?|March?|April?|May|June?|July?|August?|September?|October?|November?|December?) .*",
            RegexOptions.IgnoreCase).Groups["possibleYear"].Value;
        if (!string.IsNullOrWhiteSpace(possibleTitleYear))
            if (int.TryParse(possibleTitleYear, out var convertedYear))
                if (convertedYear >= 1826 && convertedYear <= DateTime.Now.Year)
                    toReturn.Folder = convertedYear.ToString("F0");

        if (string.IsNullOrWhiteSpace(toReturn.Folder))
            toReturn.Folder = toReturn.PhotoCreatedOn.Year.ToString("F0");

        return (GenerationReturn.Success($"Parsed Photo Metadata for {selectedFile.FullName} without error"), toReturn);
    }


    public static async Task<(GenerationReturn generationReturn, PhotoContent? photoContent)> SaveAndGenerateHtml(
        PhotoContent toSave, FileInfo selectedFile, bool overwriteExistingFiles, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave, selectedFile).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        await FileManagement.WriteSelectedPhotoContentFileToMediaArchive(selectedFile).ConfigureAwait(false);
        await Db.SavePhotoContent(toSave).ConfigureAwait(false);
        await WritePhotoFromMediaArchiveToLocalSite(toSave, overwriteExistingFiles, progress).ConfigureAwait(false);
        await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteLocalDbJson(toSave).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Photo Generator", DataNotificationContentType.Photo,
            DataNotificationUpdateType.LocalContent, new List<Guid> { toSave.ContentId });

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<(GenerationReturn generationReturn, PhotoContent? photoContent)> SaveToDb(
        PhotoContent toSave, FileInfo selectedFile)
    {
        var validationReturn = await Validate(toSave, selectedFile).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        await FileManagement.WriteSelectedPhotoContentFileToMediaArchive(selectedFile).ConfigureAwait(false);
        await Db.SavePhotoContent(toSave).ConfigureAwait(false);

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

        var commonContentCheck =
            await CommonContentValidation.ValidateContentCommon(photoContent).ConfigureAwait(false);
        if (!commonContentCheck.Valid)
            return GenerationReturn.Error(commonContentCheck.Explanation, photoContent.ContentId);

        var latitudeCheck = await CommonContentValidation.LatitudeValidationWithNullOk(photoContent.Latitude);
        if (!latitudeCheck.Valid)
            return GenerationReturn.Error(latitudeCheck.Explanation, photoContent.ContentId);

        var longitudeCheck = await CommonContentValidation.LongitudeValidationWithNullOk(photoContent.Longitude);
        if (!longitudeCheck.Valid)
            return GenerationReturn.Error(longitudeCheck.Explanation, photoContent.ContentId);

        var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(photoContent.UpdateNotesFormat);
        if (!updateFormatCheck.Valid)
            return GenerationReturn.Error(updateFormatCheck.Explanation, photoContent.ContentId);

        selectedFile.Refresh();

        var photoFileValidation = await CommonContentValidation
            .PhotoFileValidation(selectedFile, photoContent.ContentId).ConfigureAwait(false);

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

        var targetFile = new FileInfo(Path.Combine(userSettings.LocalSitePhotoContentDirectory(photoContent).FullName,
            photoContent.OriginalFileName));

        if (targetFile.Exists && forcedResizeOverwriteExistingFiles)
        {
            targetFile.Delete();
            targetFile.Refresh();
        }

        if (!targetFile.Exists) await sourceFile.CopyToAndLogAsync(targetFile.FullName).ConfigureAwait(false);

        PictureResizing.DeleteSupportedPictureFilesInDirectoryOtherThanOriginalFile(photoContent, progress);

        PictureResizing.CleanDisplayAndSrcSetFilesInPhotoDirectory(photoContent, forcedResizeOverwriteExistingFiles,
            progress);

        await PictureResizing.ResizeForDisplayAndSrcset(photoContent, forcedResizeOverwriteExistingFiles, progress)
            .ConfigureAwait(false);
    }
}