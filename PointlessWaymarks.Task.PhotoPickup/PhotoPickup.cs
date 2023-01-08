using System.Text.Json;
using Microsoft.Toolkit.Uwp.Notifications;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.Task.PhotoPickup;

public class PhotoPickup
{
    public async System.Threading.Tasks.Task PickupPhotos(string settingsFile)
    {
        if (string.IsNullOrWhiteSpace(settingsFile))
        {
            Log.Error("Settings File is Null or Whitespace?");
            return;
        }

        settingsFile = settingsFile.Trim();

        var settingsFileInfo = new FileInfo(settingsFile);

        if (!settingsFileInfo.Exists)
        {
            Log.Error($"Settings File {settingsFile} Does Not Exist?");
            return;
        }

        PhotoPickupSettings? settings;
        try
        {
            var settingsFileJsonString = await File.ReadAllTextAsync(settingsFileInfo.FullName);
            var tryReadSettings =
                JsonSerializer.Deserialize<PhotoPickupSettings>(settingsFileJsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tryReadSettings == null)
            {
                Log.Error("Settings file {settingsFile} deserialized into a null object - is the format correct?",
                    settingsFile);
                return;
            }

            settings = tryReadSettings;

            Log.ForContext("settings",
                settings.Dump()).Information($"Using settings from {settingsFileInfo.FullName}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception reading settings file {settingsFile}", settingsFile);
            return;
        }

        var pickupDirectory = new DirectoryInfo(settings.PhotoPickupDirectory);

        if (!pickupDirectory.Exists)
        {
            Log.Error($"The specified Photo Pick Up Directory {pickupDirectory.FullName} does not exist?",
                settingsFile);
            return;
        }

        var jpgFiles = pickupDirectory.EnumerateFiles("*").Where(FileAndFolderTools.PictureFileTypeIsSupported)
            .OrderBy(x => x.Name).ToList();

        if (!jpgFiles.Any())
        {
            Log.Information($"No jpg/jpeg files found in {pickupDirectory.FullName}");
            return;
        }

        var siteSettingsFileInfo = new FileInfo(settings.PointlessWaymarksSiteSettingsFileFullName);

        if (!siteSettingsFileInfo.Exists)
        {
            Log.Error(
                $"The site settings file {settings.PointlessWaymarksSiteSettingsFileFullName} was specified but not found?");
            return;
        }

        var archiveDirectory = new DirectoryInfo(settings.PhotoPickupArchiveDirectory);

        if (!archiveDirectory.Exists)
            try
            {
                archiveDirectory.Create();
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"The specified Photo Archive Directory {settings.PhotoPickupArchiveDirectory} does not exist and could not be created.");
            }

        var consoleProgress = new ConsoleProgress();

        UserSettingsUtilities.SettingsFileFullName = siteSettingsFileInfo.FullName;
        var siteSettings = await UserSettingsUtilities.ReadFromCurrentSettingsFile(consoleProgress);
        siteSettings.VerifyOrCreateAllTopLevelFolders();

        await UserSettingsUtilities.EnsureDbIsPresent(consoleProgress);

        Log.Information($"Starting processing of the {jpgFiles.Count} jpg Photo Files ");

        foreach (var loopFile in jpgFiles)
        {
            var (metaGenerationReturn, metaContent) = await
                PhotoGenerator.PhotoMetadataToNewPhotoContent(loopFile, consoleProgress);

            if (string.IsNullOrWhiteSpace(metaContent.Tags)) metaContent.Tags = "photo-pickup-automated-import";

            if (metaGenerationReturn.HasError || metaContent == null)
            {
                Log.ForContext("metaGenerationReturn", metaGenerationReturn.SafeObjectDump()).Error(
                    $"Error With Metadata for Photo {loopFile.FullName} - {metaGenerationReturn.GenerationNote} - {metaGenerationReturn.Exception?.Message}");
                continue;
            }

            FileInfo? renamedFile;

            if (settings.RenameFileToTitle)
                renamedFile =
                    await FileAndFolderTools.TryAutoRenameFileForProgramConventions(loopFile, metaContent.Title!);
            else
                renamedFile = await FileAndFolderTools.TryAutoCleanRenameFileForProgramConventions(loopFile);

            if (renamedFile is null)
            {
                Log.Information($"Error with Filename - skipping {loopFile.Name}");
                continue;
            }

            var uniqueRenamedFile = await ToPhotoFileNameNotInDb(renamedFile);

            metaContent.OriginalFileName = uniqueRenamedFile.Name;

            var (saveGenerationReturn, _) = await PhotoGenerator.SaveAndGenerateHtml(metaContent, uniqueRenamedFile,
                true,
                null, consoleProgress);

            //Clean up renamed files as needed
            if (uniqueRenamedFile.FullName != renamedFile.FullName && uniqueRenamedFile.FullName != loopFile.FullName)
                renamedFile.MoveToWithUniqueName(Path.Combine(archiveDirectory.FullName, uniqueRenamedFile.Name));
            if (renamedFile.FullName != loopFile.FullName)
                renamedFile.MoveToWithUniqueName(Path.Combine(archiveDirectory.FullName, renamedFile.Name));

            if (saveGenerationReturn.HasError)
            {
                Log.ForContext("saveGenerationReturn", saveGenerationReturn.SafeObjectDump()).Error(
                    $"Error Saving Photo {uniqueRenamedFile.FullName} - {saveGenerationReturn.GenerationNote} - {saveGenerationReturn.Exception?.Message}");

                new ToastContentBuilder()
                    .AddText($"{UserSettingsSingleton.CurrentSettings().SiteName} - Photo Pickup Error with {uniqueRenamedFile} - {saveGenerationReturn.GenerationNote}")
                    .AddAttributionText("Pointless Waymarks Project - Photo Pickup Task")
                    .Show();


            }
            else
            {
                renamedFile.MoveToWithUniqueName(Path.Combine(archiveDirectory.FullName, loopFile.Name));

                new ToastContentBuilder()
                    .AddText($"{UserSettingsSingleton.CurrentSettings().SiteName} - Photo Added '{metaContent.Title}'")
                    .AddAttributionText("Pointless Waymarks Project - Photo Pickup Task")
                    .Show();
            }
        }
    }


    /// <summary>
    ///     This routine tries to return a FileInfo for a filename that is not in use by Photo Content in the Db, matches
    ///     program conventions and is unique in the current directory.
    /// </summary>
    /// <param name="baseFile"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static async Task<FileInfo> ToPhotoFileNameNotInDb(FileInfo baseFile)
    {
        var searchContext = await Db.Context();

        var fileExistsInDatabase = await searchContext.PhotoFilenameExistsInDatabase(baseFile.Name, null);
        if (!fileExistsInDatabase) return baseFile;

        var file = new FileInfo(baseFile.FullName);

        var numberLimit = 999;
        var filePostfix = 0;
        ;
        while ((file.Exists || fileExistsInDatabase) && filePostfix <= numberLimit)
        {
            numberLimit++;
            filePostfix++;

            var newFileName =
                SlugTools.CreateSlug(false,
                    $"{Path.GetFileNameWithoutExtension(baseFile.Name)}-{filePostfix:000}{baseFile.Extension}");

            fileExistsInDatabase = await searchContext.PhotoFilenameExistsInDatabase(newFileName, null);

            file = new FileInfo(Path.Combine(baseFile.DirectoryName ?? string.Empty,
                newFileName));
        }

        if (!file.Exists)
        {
            baseFile.CopyTo(file.FullName);
            file.Refresh();
            return file;
        }


        var randomPostfixLimit = 50;
        var randomPostfixCounter = 0;
        while ((file.Exists || fileExistsInDatabase) && randomPostfixCounter <= randomPostfixLimit)
        {
            randomPostfixLimit++;
            randomPostfixCounter++;

            var postFix = SlugTools.RandomLowerCaseString(6);

            var newFileName = SlugTools.CreateSlug(false,
                $"{Path.GetFileNameWithoutExtension(baseFile.Name)}-{postFix}{baseFile.Extension}");

            fileExistsInDatabase = await searchContext.PhotoFilenameExistsInDatabase(newFileName, null);

            file = new FileInfo(Path.Combine(baseFile.DirectoryName ?? string.Empty,
                newFileName));
        }

        if (!file.Exists)
        {
            baseFile.CopyTo(file.FullName);
            file.Refresh();
            return file;
        }

        throw new Exception("Can not create a Unique Directory for {fullName}");
    }
}