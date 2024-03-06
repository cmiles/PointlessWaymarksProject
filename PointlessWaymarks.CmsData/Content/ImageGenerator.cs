using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Xmp;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;
using XmpCore;

namespace PointlessWaymarks.CmsData.Content;

public static class ImageGenerator
{
    public static async Task GenerateHtml(ImageContent toGenerate, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Image Content - Generate HTML for {toGenerate.Title}");

        var htmlContext = new SingleImagePage(toGenerate) { GenerationVersion = generationVersion };

        await htmlContext.WriteLocalHtml().ConfigureAwait(false);
    }

    public static async Task<(GenerationReturn generationReturn, ImageMetadata? metadata)> ImageMetadataFromFile(
        FileInfo selectedFile, IProgress<string>? progress = null)
    {
        //2023/7/9 - This is a reduced version of the Photo Metadata method in the PhotoGenerator - you may
        //find additional notes there (which may or may not apply since images, purposely, don't have the
        //extensive metadata that Photographs do).
        progress?.Report("Starting Metadata Processing");

        selectedFile.Refresh();

        if (!selectedFile.Exists) return (GenerationReturn.Error("File Does Not Exist?"), null);

        var toReturn = new ImageMetadata();

        progress?.Report("Getting Directories");

        var metadataDirectories = ImageMetadataReader.ReadMetadata(selectedFile.FullName);
        var exifIfdDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<ExifIfd0Directory>()
            .FirstOrDefault();
        var iptcDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<IptcDirectory>()
            .FirstOrDefault();
        var xmpDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<XmpDirectory>()
            .FirstOrDefault();

        var createdOn =
            await FileMetadataEmbeddedTools.CreatedOnLocalAndUtc(metadataDirectories);

        var imageCreatedOn = createdOn.createdOnLocal ?? DateTime.Now;

        var tags = new List<string>();

        toReturn.Title = xmpDirectory?.XmpMeta?.GetArrayItem(XmpConstants.NsDC, "title", 1)?.Value;

        if (string.IsNullOrWhiteSpace(toReturn.Title))
            toReturn.Title = iptcDirectory?.GetDescription(IptcDirectory.TagObjectName) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(toReturn.Title))
            toReturn.Title = Path.GetFileNameWithoutExtension(selectedFile.Name).Replace("-", " ").Replace("_", " ")
                .CamelCaseToSpacedString();

        if (string.IsNullOrWhiteSpace(toReturn.Title)) toReturn.Title = string.Empty;

        var dateTimeFromTitle = DateTimeTools.DateOnlyFromTitleStringByConvention(toReturn.Title);

        if (dateTimeFromTitle == null)
        {
            progress?.Report("Unable to parse a date from title");
            toReturn.Title = string.IsNullOrWhiteSpace(toReturn.Title)
                ? imageCreatedOn.ToString("yyyy MMMM dd h-mm-ss tt")
                : $"{imageCreatedOn:yyyy} {imageCreatedOn:MMMM} {toReturn.Title.TrimNullToEmpty()}";
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

        return (GenerationReturn.Success($"Parsed Image Metadata for {selectedFile.FullName} without error"), toReturn);
    }

    public static async Task<(GenerationReturn generationReturn, ImageContent? imageContent)> SaveAndGenerateHtml(
        ImageContent toSave, FileInfo selectedFile, bool overwriteExistingFiles, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave, selectedFile).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        toSave.OriginalFileName = selectedFile.Name;
        await FileManagement.WriteSelectedImageContentFileToMediaArchive(selectedFile).ConfigureAwait(false);
        await Db.SaveImageContent(toSave).ConfigureAwait(false);
        await WriteImageFromMediaArchiveToLocalSite(toSave, overwriteExistingFiles, progress).ConfigureAwait(false);
        await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteImageContentData(toSave).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Image Generator", DataNotificationContentType.Image,
            DataNotificationUpdateType.LocalContent, new List<Guid> { toSave.ContentId });

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<GenerationReturn> Validate(ImageContent? imageContent, FileInfo? selectedFile)
    {
        if (imageContent == null)
            return GenerationReturn.Error("Null Image Content submitted to Validate?");

        if (selectedFile == null)
            return GenerationReturn.Error("No Image File submitted to Validate?", imageContent.ContentId);

        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                imageContent.ContentId);

        var mediaArchiveCheck = UserSettingsUtilities.ValidateLocalMediaArchive();
        if (!mediaArchiveCheck.Valid)
            return GenerationReturn.Error($"Problem with Media Archive: {mediaArchiveCheck.Explanation}",
                imageContent.ContentId);

        var commonContentCheck =
            await CommonContentValidation.ValidateContentCommon(imageContent).ConfigureAwait(false);
        if (!commonContentCheck.Valid)
            return GenerationReturn.Error(commonContentCheck.Explanation, imageContent.ContentId);

        var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(imageContent.UpdateNotesFormat);
        if (!updateFormatCheck.Valid)
            return GenerationReturn.Error(updateFormatCheck.Explanation, imageContent.ContentId);

        selectedFile.Refresh();

        if (!selectedFile.Exists)
            return GenerationReturn.Error("Selected File doesn't exist?", imageContent.ContentId);

        if (!FileAndFolderTools.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(selectedFile.Name)))
            return GenerationReturn.Error("Limit File Names to A-Z a-z - . _", imageContent.ContentId);

        if (!FileAndFolderTools.PictureFileTypeIsSupported(selectedFile))
            return GenerationReturn.Error("The file doesn't appear to be a supported file type.",
                imageContent.ContentId);

        if (await (await Db.Context().ConfigureAwait(false))
            .ImageFilenameExistsInDatabase(selectedFile.Name, imageContent.ContentId).ConfigureAwait(false))
            return GenerationReturn.Error(
                "This filename already exists in the database - image file names must be unique.",
                imageContent.ContentId);

        return GenerationReturn.Success("Image Content Validation Successful");
    }

    public static async Task WriteImageFromMediaArchiveToLocalSite(ImageContent imageContent,
        bool forcedResizeOverwriteExistingFiles, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(imageContent.OriginalFileName)) return;

        var userSettings = UserSettingsSingleton.CurrentSettings();

        var sourceFile = new FileInfo(Path.Combine(userSettings.LocalMediaArchiveImageDirectory().FullName,
            imageContent.OriginalFileName));

        var targetFile = new FileInfo(Path.Combine(
            userSettings.LocalSiteImageContentDirectory(imageContent).FullName, imageContent.OriginalFileName));

        if (targetFile.Exists && forcedResizeOverwriteExistingFiles)
        {
            targetFile.Delete();
            targetFile.Refresh();
        }

        if (!targetFile.Exists) await sourceFile.CopyToAndLogAsync(targetFile.FullName).ConfigureAwait(false);

        PictureResizing.DeleteSupportedPictureFilesInDirectoryOtherThanOriginalFile(imageContent, progress);

        PictureResizing.CleanDisplayAndSrcSetFilesInImageDirectory(imageContent, forcedResizeOverwriteExistingFiles,
            progress);

        await PictureResizing.ResizeForDisplayAndSrcset(imageContent, forcedResizeOverwriteExistingFiles, progress)
            .ConfigureAwait(false);
    }
}