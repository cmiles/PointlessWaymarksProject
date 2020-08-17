using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Html.ImageHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Content
{
    public static class ImageGenerator
    {
        public static void GenerateHtml(ImageContent toGenerate, DateTime? generationVersion, IProgress<string> progress)
        {
            progress?.Report($"Image Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleImagePage(toGenerate) {GenerationVersion = generationVersion};

            htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, ImageContent imageContent)> SaveAndGenerateHtml(
            ImageContent toSave, FileInfo selectedFile, bool overwriteExistingFiles, DateTime? generationVersion, IProgress<string> progress)
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

            await DataNotifications.PublishDataNotification("Image Generator", DataNotificationContentType.Image,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (await GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(ImageContent imageContent, FileInfo selectedFile)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    imageContent.ContentId);

            var mediaArchiveCheck = UserSettingsUtilities.ValidateLocalMediaArchive();
            if (!mediaArchiveCheck.Item1)
                return await GenerationReturn.Error($"Problem with Media Archive: {mediaArchiveCheck.Item2}",
                    imageContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(imageContent);
            if (!commonContentCheck.valid)
                return await GenerationReturn.Error(commonContentCheck.explanation, imageContent.ContentId);

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(imageContent.UpdateNotesFormat);
            if (!updateFormatCheck.isValid)
                return await GenerationReturn.Error(updateFormatCheck.explanation, imageContent.ContentId);

            selectedFile.Refresh();

            if (!selectedFile.Exists)
                return await GenerationReturn.Error("Selected File doesn't exist?", imageContent.ContentId);

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(selectedFile.Name)))
                return await GenerationReturn.Error("Limit File Names to A-Z a-z - . _", imageContent.ContentId);

            if (!FolderFileUtility.PictureFileTypeIsSupported(selectedFile))
                return await GenerationReturn.Error("The file doesn't appear to be a supported file type.",
                    imageContent.ContentId);

            if (await (await Db.Context()).ImageFilenameExistsInDatabase(selectedFile.Name, imageContent.ContentId))
                return await GenerationReturn.Error(
                    "This filename already exists in the database - image file names must be unique.",
                    imageContent.ContentId);

            return await GenerationReturn.Success("Image Content Validation Successful");
        }

        public static async Task WriteImageFromMediaArchiveToLocalSite(ImageContent imageContent,
            bool forcedResizeOverwriteExistingFiles, IProgress<string> progress)
        {
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

            if (!targetFile.Exists) sourceFile.CopyTo(targetFile.FullName);

            PictureResizing.DeleteSupportedPictureFilesInDirectoryOtherThanOriginalFile(imageContent, progress);

            PictureResizing.CleanDisplayAndSrcSetFilesInImageDirectory(imageContent, forcedResizeOverwriteExistingFiles,
                progress);

            await PictureResizing.ResizeForDisplayAndSrcset(imageContent, forcedResizeOverwriteExistingFiles, progress);
        }
    }
}