using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html;
using PointlessWaymarks.CmsData.Html.FileHtml;
using PointlessWaymarks.CmsData.Json;
using Serilog;

namespace PointlessWaymarks.CmsData.Content
{
    public static class FileGenerator
    {
        public static void GenerateHtml(FileContent toGenerate, DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            progress?.Report($"File Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleFilePage(toGenerate) {GenerationVersion = generationVersion};

            htmlContext.WriteLocalHtml();
        }


        public static async Task<(GenerationReturn generationReturn, FileContent? fileContent)> SaveAndGenerateHtml(
            FileContent toSave, FileInfo selectedFile, bool overwriteExistingFiles, DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            var validationReturn = await Validate(toSave, selectedFile);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            toSave.OriginalFileName = selectedFile.Name;
            FileManagement.WriteSelectedFileContentFileToMediaArchive(selectedFile);
            await Db.SaveFileContent(toSave);
            WriteFileFromMediaArchiveToLocalSite(toSave, overwriteExistingFiles);
            GenerateHtml(toSave, generationVersion, progress);
            await Export.WriteLocalDbJson(toSave, progress);

            DataNotifications.PublishDataNotification("File Generator", DataNotificationContentType.File,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(FileContent fileContent, FileInfo selectedFile)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Valid)
                return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                    fileContent.ContentId);

            var mediaArchiveCheck = UserSettingsUtilities.ValidateLocalMediaArchive();
            if (!mediaArchiveCheck.Valid)
                return GenerationReturn.Error($"Problem with Media Archive: {mediaArchiveCheck.Explanation}",
                    fileContent.ContentId);

            var (valid, explanation) = await CommonContentValidation.ValidateContentCommon(fileContent);
            if (!valid)
                return GenerationReturn.Error(explanation, fileContent.ContentId);

            var (isValid, s) = CommonContentValidation.ValidateUpdateContentFormat(fileContent.UpdateNotesFormat);
            if (!isValid)
                return GenerationReturn.Error(s, fileContent.ContentId);

            selectedFile.Refresh();

            if (!selectedFile.Exists)
                return GenerationReturn.Error("Selected File doesn't exist?", fileContent.ContentId);

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(selectedFile.Name)))
                return GenerationReturn.Error("Limit File Names to A-Z a-z - . _", fileContent.ContentId);

            if (await (await Db.Context()).FileFilenameExistsInDatabase(selectedFile.Name, fileContent.ContentId))
                return GenerationReturn.Error(
                    "This filename already exists in the database - file names must be unique.", fileContent.ContentId);

            return GenerationReturn.Success("File Content Validation Successful");
        }

        public static void WriteFileFromMediaArchiveToLocalSite(FileContent fileContent, bool overwriteExisting)
        {
            if (string.IsNullOrWhiteSpace(fileContent.OriginalFileName))
            {
                Log.Warning(
                    $"FileContent with a blank {nameof(fileContent.OriginalFileName)} was submitted to WriteFileFromMediaArchiveToLocalSite");
                return;
            }

            var userSettings = UserSettingsSingleton.CurrentSettings();

            var sourceFile = new FileInfo(Path.Combine(userSettings.LocalMediaArchiveFileDirectory().FullName,
                fileContent.OriginalFileName));

            var targetFile = new FileInfo(Path.Combine(userSettings.LocalSiteFileContentDirectory(fileContent).FullName,
                fileContent.OriginalFileName));

            if (targetFile.Exists && overwriteExisting)
            {
                targetFile.Delete();
                targetFile.Refresh();
            }

            if (!targetFile.Exists) sourceFile.CopyToAndLog(targetFile.FullName);
        }
    }
}