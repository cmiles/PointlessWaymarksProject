using System.Text.Json;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
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

        var jpgFiles = pickupDirectory.EnumerateFiles("*").Where(FolderFileUtility.PictureFileTypeIsSupported)
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

        UserSettingsUtilities.SettingsFileFullName = siteSettingsFileInfo!.FullName;
        var siteSettings = await UserSettingsUtilities.ReadFromCurrentSettingsFile(consoleProgress);
        siteSettings.VerifyOrCreateAllTopLevelFolders();

        await UserSettingsUtilities.EnsureDbIsPresent(consoleProgress);

        Log.Information($"Starting processing of the {jpgFiles.Count} jpg Photo Files ");

        foreach (var loopFile in jpgFiles)
        {
            var (metaGenerationReturn, metaContent) = await
                PhotoGenerator.PhotoMetadataToNewPhotoContent(loopFile, consoleProgress);

            if (metaGenerationReturn.HasError || metaContent == null)
            {
                Log.ForContext("metaGenerationReturn", metaGenerationReturn.SafeObjectDump()).Error(
                    $"Error Saving Photo {loopFile.FullName} - {metaGenerationReturn.GenerationNote} - {metaGenerationReturn.Exception?.Message}");
                continue;
            }

            var (saveGenerationReturn, _) = await PhotoGenerator.SaveAndGenerateHtml(metaContent, loopFile, true,
                null, consoleProgress);

            if (saveGenerationReturn.HasError)
            {
                Log.ForContext("saveGenerationReturn", saveGenerationReturn.SafeObjectDump()).Error(
                    $"Error Saving Photo {loopFile.FullName} - {saveGenerationReturn.GenerationNote} - {saveGenerationReturn.Exception?.Message}");
                continue;
            }

            try
            {
                loopFile.MoveTo(Path.Combine(archiveDirectory.FullName, loopFile.Name), true);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to move file to Archive Directory - {loopFile.FullName}");
            }
        }
    }
}