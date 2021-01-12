using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html;
using PointlessWaymarks.CmsData.Html.ImageHtml;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.Content
{
    public static class ImageGenerator
    {
        public static void GenerateHtml(ImageContent toGenerate, DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            progress?.Report($"Image Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleImagePage(toGenerate) {GenerationVersion = generationVersion};

            htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, ImageContent? imageContent)> SaveAndGenerateHtml(
            ImageContent toSave, FileInfo selectedFile, bool overwriteExistingFiles, DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            var validationReturn = await Validate(toSave, selectedFile);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            toSave.OriginalFileName = selectedFile.Name;
            FileManagement.WriteSelectedImageContentFileToMediaArchive(selectedFile);
            await Db.SaveImageContent(toSave);
            await WriteImageFromMediaArchiveToLocalSite(toSave, overwriteExistingFiles, progress);
            GenerateHtml(toSave, generationVersion, progress);
            await Export.WriteLocalDbJson(toSave);

            DataNotifications.PublishDataNotification("Image Generator", DataNotificationContentType.Image,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(ImageContent imageContent, FileInfo selectedFile)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Valid)
                return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                    imageContent.ContentId);

            var mediaArchiveCheck = UserSettingsUtilities.ValidateLocalMediaArchive();
            if (!mediaArchiveCheck.Valid)
                return GenerationReturn.Error($"Problem with Media Archive: {mediaArchiveCheck.Explanation}",
                    imageContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(imageContent);
            if (!commonContentCheck.Valid)
                return GenerationReturn.Error(commonContentCheck.Explanation, imageContent.ContentId);

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(imageContent.UpdateNotesFormat);
            if (!updateFormatCheck.Valid)
                return GenerationReturn.Error(updateFormatCheck.Explanation, imageContent.ContentId);

            selectedFile.Refresh();

            if (!selectedFile.Exists)
                return GenerationReturn.Error("Selected File doesn't exist?", imageContent.ContentId);

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(selectedFile.Name)))
                return GenerationReturn.Error("Limit File Names to A-Z a-z - . _", imageContent.ContentId);

            if (!FolderFileUtility.PictureFileTypeIsSupported(selectedFile))
                return GenerationReturn.Error("The file doesn't appear to be a supported file type.",
                    imageContent.ContentId);

            if (await (await Db.Context()).ImageFilenameExistsInDatabase(selectedFile.Name, imageContent.ContentId))
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

            if (!targetFile.Exists) await sourceFile.CopyToAndLogAsync(targetFile.FullName);

            PictureResizing.DeleteSupportedPictureFilesInDirectoryOtherThanOriginalFile(imageContent, progress);

            PictureResizing.CleanDisplayAndSrcSetFilesInImageDirectory(imageContent, forcedResizeOverwriteExistingFiles,
                progress);

            await PictureResizing.ResizeForDisplayAndSrcset(imageContent, forcedResizeOverwriteExistingFiles, progress);
        }
    }
}