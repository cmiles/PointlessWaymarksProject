using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Html.FileHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Content
{
    public static class FileGenerator
    {
        public static void GenerateHtml(FileContent toGenerate, IProgress<string> progress)
        {
            progress?.Report($"File Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SingleFilePage(toGenerate);

            htmlContext.WriteLocalHtml();
        }


        public static async Task<(GenerationReturn generationReturn, FileContent fileContent)> SaveAndGenerateHtml(
            FileContent toSave, FileInfo selectedFile, bool overwriteExistingFiles, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave, selectedFile);

            if (validationReturn.HasError) return (validationReturn, null);

            toSave.OriginalFileName = selectedFile.Name;
            StructureAndMediaContent.WriteSelectedFileContentFileToMediaArchive(selectedFile);
            await Db.SaveFileContent(toSave);
            WriteFileFromMediaArchiveToLocalSite(toSave, overwriteExistingFiles, progress);
            GenerateHtml(toSave, progress);
            await Export.WriteLocalDbJson(toSave, progress);

            await DataNotifications.PublishDataNotification("File Generator", DataNotificationContentType.File,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (await GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<(GenerationReturn generationReturn, FileContent fileContent)> SaveToDb(
            FileContent toSave, FileInfo selectedFile, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave, selectedFile);

            if (validationReturn.HasError) return (validationReturn, null);

            StructureAndMediaContent.WriteSelectedFileContentFileToMediaArchive(selectedFile);
            await Db.SaveFileContent(toSave);

            return (await GenerationReturn.Success($"Saved {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(FileContent fileContent, FileInfo selectedFile)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    fileContent.ContentId);

            var mediaArchiveCheck = UserSettingsUtilities.ValidateLocalMediaArchive();
            if (!mediaArchiveCheck.Item1)
                return await GenerationReturn.Error($"Problem with Media Archive: {mediaArchiveCheck.Item2}",
                    fileContent.ContentId);

            var commonContentCheck = CommonContentValidation.ValidateContentCommon(fileContent);
            if (!commonContentCheck.Item1)
                return await GenerationReturn.Error(commonContentCheck.Item2, fileContent.ContentId);

            selectedFile.Refresh();

            if (fileContent == null) return await GenerationReturn.Error("File Content is Null?");

            if (!selectedFile.Exists)
                return await GenerationReturn.Error("Selected File doesn't exist?", fileContent.ContentId);

            if (!FolderFileUtility.IsNoUrlEncodingNeededFilename(selectedFile.Name))
                return await GenerationReturn.Error("Limit File Names to A-Z a-z - . _", fileContent.ContentId);

            if (await (await Db.Context()).FileFilenameExistsInDatabase(selectedFile.Name, fileContent.ContentId))
                return await GenerationReturn.Error(
                    "This filename already exists in the database - file names must be unique.", fileContent.ContentId);

            if (await (await Db.Context()).SlugExistsInDatabase(fileContent.Slug, fileContent.ContentId))
                return await GenerationReturn.Error("This slug already exists in the database - slugs must be unique.",
                    fileContent.ContentId);

            return await GenerationReturn.Success("File Content Validation Successful");
        }

        public static void WriteFileFromMediaArchiveToLocalSite(FileContent fileContent, bool overwriteExisting,
            IProgress<string> progress)
        {
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

            if (!targetFile.Exists) sourceFile.CopyTo(targetFile.FullName);
        }
    }
}