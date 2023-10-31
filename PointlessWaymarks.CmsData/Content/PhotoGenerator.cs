using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Xmp;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.SpatialTools;
using Serilog;
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
        FileInfo selectedFile, bool skipAdditionalTagDiscovery = false, IProgress<string>? progress = null)
    {
        progress?.Report("Starting Metadata Processing");

        selectedFile.Refresh();

        if (!selectedFile.Exists) return (GenerationReturn.Error("File Does Not Exist?"), null);

        var toReturn = new PhotoMetadata();

        progress?.Report("Getting Directories");

        var metadataDirectories = ImageMetadataReader.ReadMetadata(selectedFile.FullName);
        var exifSubIfdDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<ExifSubIfdDirectory>()
            .FirstOrDefault();
        var exifIfdDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<ExifIfd0Directory>()
            .FirstOrDefault();
        var iptcDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<IptcDirectory>()
            .FirstOrDefault();
        var xmpDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<XmpDirectory>()
            .FirstOrDefault();

        toReturn.PhotoCreatedBy = exifIfdDirectory?.GetDescription(ExifDirectoryBase.TagArtist) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(toReturn.PhotoCreatedBy))
            toReturn.PhotoCreatedBy = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "creator", 1)?.Value ??
                                      string.Empty;

        if (string.IsNullOrWhiteSpace(toReturn.PhotoCreatedBy))
            toReturn.PhotoCreatedBy = iptcDirectory?.GetDescription(IptcDirectory.TagByLine) ?? string.Empty;

        var createdOn =
            await FileMetadataEmbeddedTools.CreatedOnLocalAndUtc(metadataDirectories);

        toReturn.PhotoCreatedOn = createdOn.createdOnLocal ?? DateTime.Now;
        toReturn.PhotoCreatedOnUtc = createdOn.createdOnUtc;

        var locationInformation = await FileMetadataEmbeddedTools.LocationFromExif(metadataDirectories, true, progress);

        toReturn.Latitude = locationInformation.Latitude;
        toReturn.Longitude = locationInformation.Longitude;
        toReturn.Elevation = locationInformation.Elevation;

        var tags = new List<string>();

        if (toReturn is { Latitude: not null, Longitude: not null } && !skipAdditionalTagDiscovery)
            try
            {
                var stateCounty =
                    await StateCountyService.GetStateCounty(toReturn.Latitude.Value,
                        toReturn.Longitude.Value);
                if (!string.IsNullOrWhiteSpace(stateCounty.state))
                {
                    tags.Add(stateCounty.state);
                    tags.Add("United States");
                }

                if (!string.IsNullOrWhiteSpace(stateCounty.county)) tags.Add(stateCounty.county);
            }
            catch (Exception e)
            {
                Log.ForContext("hint",
                        "It is expected that this network service will occasionally fail - this error is logged but not thrown in the program and the failure simply appears as no county and state added via this service...")
                    .ForContext("exception", e).Information("StateCountyService.GetStateCounty Failure");
                progress?.Report($"Ignored Service Failure getting State/County - {e.Message}");
            }

        if (toReturn is { Latitude: not null, Longitude: not null } &&
            UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagOnImport &&
            !string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile) &&
            !skipAdditionalTagDiscovery)
            try
            {
                var pointFeature = new Feature(
                    new Point(toReturn.Longitude.Value, toReturn.Latitude.Value),
                    new AttributesTable());

                tags.AddRange(pointFeature.IntersectionTags(
                    UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
                    CancellationToken.None, progress));
            }
            catch (Exception e)
            {
                Log.Error(e, "Silent Error with FeatureIntersectionTags in Photo Metadata Extraction");
            }

        var isoString = exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagIsoEquivalent);
        if (!string.IsNullOrWhiteSpace(isoString)) toReturn.Iso = int.Parse(isoString);

        toReturn.CameraMake = exifIfdDirectory?.GetDescription(ExifDirectoryBase.TagMake) ?? exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagMake) ?? string.Empty;
        toReturn.CameraModel = exifIfdDirectory?.GetDescription(ExifDirectoryBase.TagModel) ?? exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagModel) ?? string.Empty;
        toReturn.FocalLength = exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength) ?? string.Empty;

        toReturn.Lens = exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagLensModel) ?? string.Empty;

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

        toReturn.Aperture = exifSubIfdDirectory?.GetDescription(ExifDirectoryBase.TagAperture) ?? string.Empty;

        toReturn.License = exifIfdDirectory?.GetDescription(ExifDirectoryBase.TagCopyright) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(toReturn.License))
            toReturn.License = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "rights", 1)?.Value ??
                               string.Empty;

        if (string.IsNullOrWhiteSpace(toReturn.License))
            toReturn.License = iptcDirectory?.GetDescription(IptcDirectory.TagCopyrightNotice) ?? string.Empty;

        // ReSharper disable once InlineOutVariableDeclaration - Better to establish type of shutterValue explicitly
        Rational shutterValue;
        // ReSharper disable once InlineOutVariableDeclaration - Better to establish type of exposureValue explicitly
        Rational exposureValue;
        if (exifSubIfdDirectory?.TryGetRational(ExifDirectoryBase.TagShutterSpeed, out shutterValue) ?? false)
            toReturn.ShutterSpeed = ExifHelpers.ShutterSpeedToHumanReadableString(shutterValue);
        else if (exifSubIfdDirectory?.TryGetRational(ExifDirectoryBase.TagExposureTime, out exposureValue) ?? false)
            toReturn.ShutterSpeed = ExifHelpers.ExposureTimeToHumanReadableString(exposureValue);
        else
            toReturn.ShutterSpeed = string.Empty;

        //The XMP data - vs the IPTC - will hold the full Title for a very long title (the IPTC will be truncated) -
        //for a 'from Lightroom with no other concerns' export Title makes the most sense, but there are other possible
        //metadata fields to pull from that could be relevant in other contexts.
        toReturn.Title = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "title", 1)?.Value;

        if (string.IsNullOrWhiteSpace(toReturn.Title))
            toReturn.Title = iptcDirectory?.GetDescription(IptcDirectory.TagObjectName) ?? string.Empty;
        //Use a variety of guess on common file names and make that the title - this could result in an initial title
        //like DSC001 style out of camera names but after having experimented with loading files I think 'default' is better
        //than an invalid blank.
        if (string.IsNullOrWhiteSpace(toReturn.Title))
            toReturn.Title = Path.GetFileNameWithoutExtension(selectedFile.Name).Replace("-", " ").Replace("_", " ")
                .CamelCaseToSpacedString();

        if (string.IsNullOrWhiteSpace(toReturn.Title)) toReturn.Title = string.Empty;

        var dateTimeFromTitle = DateTimeTools.DateOnlyFromTitleStringByConvention(toReturn.Title);

        if (dateTimeFromTitle == null)
        {
            progress?.Report("Unable to parse a date from title");
            toReturn.Title = string.IsNullOrWhiteSpace(toReturn.Title)
                ? toReturn.PhotoCreatedOn.ToString("yyyy MMMM dd h-mm-ss tt")
                : $"{toReturn.PhotoCreatedOn:yyyy} {toReturn.PhotoCreatedOn:MMMM} {toReturn.Title.TrimNullToEmpty()}";
            toReturn.Summary = iptcDirectory?.GetDescription(IptcDirectory.TagObjectName) ?? string.Empty;
        }
        else
        {
            progress?.Report(
                $"Parsed title - {dateTimeFromTitle.Value.titleDate:yyyy} {dateTimeFromTitle.Value.titleDate:MMMM} {toReturn.Title.TrimNullToEmpty()}");
            toReturn.Title =
                $"{dateTimeFromTitle.Value.titleDate:yyyy} {dateTimeFromTitle.Value.titleDate:MMMM} {dateTimeFromTitle.Value.titleWithDateRemoved.TrimNullToEmpty()}";
            toReturn.Summary = dateTimeFromTitle.Value.titleWithDateRemoved;
        }

        if (string.IsNullOrWhiteSpace(toReturn.Summary)) toReturn.Summary = toReturn.Title;

        //Order is important here - the title supplies the summary in the code above - but overwrite that if there is a
        //description.
        var description = exifIfdDirectory?.GetDescription(ExifDirectoryBase.TagImageDescription) ?? string.Empty;
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

        tags.AddRange(FileMetadataEmbeddedTools.KeywordsFromExif(metadataDirectories, true));

        toReturn.Tags = tags.Any() ? Db.TagListJoin(tags) : string.Empty;

        return (GenerationReturn.Success($"Parsed Photo Metadata for {selectedFile.FullName} without error"), toReturn);
    }

    public static async Task<(GenerationReturn, PhotoContent?)> PhotoMetadataToNewPhotoContent(FileInfo selectedFile,
        IProgress<string> progress, string? photoContentCreatedBy = null)
    {
        selectedFile.Refresh();

        if (!selectedFile.Exists) return (GenerationReturn.Error("File Does Not Exist?"), null);

        var (generationReturn, metadata) = await PhotoMetadataFromFile(selectedFile, false, progress);

        if (generationReturn.HasError) return (generationReturn, null);

        var toReturn = PhotoContent.CreateInstance();

        toReturn.InjectFrom(metadata);

        toReturn.OriginalFileName = selectedFile.Name;
        toReturn.CreatedBy = string.IsNullOrWhiteSpace(photoContentCreatedBy)
            ? UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
            : photoContentCreatedBy.Trim();
        toReturn.Slug = SlugTools.CreateSlug(true, toReturn.Title);
        toReturn.BodyContentFormat = ContentFormatDefaults.Content.ToString();
        toReturn.UpdateNotesFormat = ContentFormatDefaults.Content.ToString();
        toReturn.ShowPhotoPosition = UserSettingsSingleton.CurrentSettings().PhotoPagesShowPositionByDefault;
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

        if (selectedFile.Name != photoContent.OriginalFileName)
            return GenerationReturn.Error("The Photo Content Original File Name and Selected File are mis-matched.");

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