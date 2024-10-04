using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.CmsData.ContentGeneration;

public static class FileGenerator
{
    public static async Task GenerateHtml(FileContent toGenerate, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report($"File Content - Generate HTML for {toGenerate.Title}");

        var htmlContext = new SingleFilePage(toGenerate) { GenerationVersion = generationVersion };

        await htmlContext.WriteLocalHtml().ConfigureAwait(false);
    }


    public static async Task<(GenerationReturn generationReturn, FileContent? fileContent)> SaveAndGenerateHtml(
        FileContent toSave, FileInfo selectedFile, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave, selectedFile).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        toSave.OriginalFileName = selectedFile.Name;
        await FileManagement.WriteSelectedFileContentFileToMediaArchive(selectedFile).ConfigureAwait(false);
        await Db.SaveFileContent(toSave).ConfigureAwait(false);
        await WriteFileFromMediaArchiveToLocalSiteIfNeeded(toSave).ConfigureAwait(false);
        await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteFileContentData(toSave, progress).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("File Generator", DataNotificationContentType.File,
            DataNotificationUpdateType.LocalContent, [toSave.ContentId]);

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<GenerationReturn> Validate(FileContent? fileContent, FileInfo? selectedFile)
    {
        if (fileContent == null)
            return GenerationReturn.Error("Null File Content submitted to Validate?");

        if (selectedFile == null)
            return GenerationReturn.Error("No File submitted to Validate?", fileContent.ContentId);

        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                fileContent.ContentId);

        var mediaArchiveCheck = UserSettingsUtilities.ValidateLocalMediaArchive();
        if (!mediaArchiveCheck.Valid)
            return GenerationReturn.Error($"Problem with Media Archive: {mediaArchiveCheck.Explanation}",
                fileContent.ContentId);

        var (valid, explanation) =
            await CommonContentValidation.ValidateContentCommon(fileContent).ConfigureAwait(false);
        if (!valid)
            return GenerationReturn.Error(explanation, fileContent.ContentId);

        var (userMainImageIsValid, userMainImageExplanation) =
            await CommonContentValidation.ValidateUserMainPicture(fileContent.UserMainPicture).ConfigureAwait(false);
        if (!userMainImageIsValid)
            return GenerationReturn.Error(userMainImageExplanation, fileContent.ContentId);

        var (isValid, s) = CommonContentValidation.ValidateUpdateContentFormat(fileContent.UpdateNotesFormat);
        if (!isValid)
            return GenerationReturn.Error(s, fileContent.ContentId);

        selectedFile.Refresh();

        if (!selectedFile.Exists)
            return GenerationReturn.Error("Selected File doesn't exist?", fileContent.ContentId);

        if (!FileAndFolderTools.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(selectedFile.Name)))
            return GenerationReturn.Error("Limit File Names to A-Z a-z - . _", fileContent.ContentId);

        if (await (await Db.Context().ConfigureAwait(false))
            .FileFilenameExistsInDatabase(selectedFile.Name, fileContent.ContentId).ConfigureAwait(false))
            return GenerationReturn.Error(
                "This filename already exists in the database - file names must be unique.", fileContent.ContentId);

        return GenerationReturn.Success("File Content Validation Successful");
    }

    public static async Task WriteFileFromMediaArchiveToLocalSiteIfNeeded(FileContent fileContent)
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

        if (!targetFile.Exists || sourceFile.CalculateMD5() != targetFile.CalculateMD5())
        {
            if (targetFile.Exists)
            {
                targetFile.Delete();
                targetFile.Refresh();
            }

            await sourceFile.CopyToAndLog(targetFile.FullName).ConfigureAwait(false);
        }
    }
}