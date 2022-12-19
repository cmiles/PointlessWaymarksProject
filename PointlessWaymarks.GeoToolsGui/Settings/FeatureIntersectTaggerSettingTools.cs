#region

using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.GeoToolsGui.Models;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

public static class FeatureIntersectTaggerSettingTools
{
    public static async Task<FileInfo> DefaultSettingsFile()
    {
        var settingsFile =
            new FileInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                "PwGtgFeatureIntersectTaggerSettings.json"));

        if (!settingsFile.Exists)
        {
            var blankSettings = new FeatureIntersectTaggerSettings();
            var serializedSettings =
                JsonSerializer.Serialize(blankSettings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
            settingsFile.Refresh();
        }

        return settingsFile;
    }

    public static async Task<DirectoryInfo?> GetPadUsDirectory()
    {
        var directory = (await ReadSettings()).PadUsDirectory;

        if (string.IsNullOrEmpty(directory)) return null;

        return new DirectoryInfo(directory);
    }

    public static async Task<FeatureIntersectTaggerSettings> ReadSettings()
    {
        FeatureIntersectTaggerSettings? settings;

        return JsonSerializer.Deserialize<FeatureIntersectTaggerSettings>(
                   await File.ReadAllTextAsync((await DefaultSettingsFile()).FullName)) ??
               new FeatureIntersectTaggerSettings();
    }

    public static async Task<FeatureIntersectTaggerSettings> SetFeatureFiles(
        List<FeatureFileViewModel> featureFiles)
    {
        var settings = await ReadSettings();

        settings.FeatureIntersectFiles = featureFiles.OrderBy<FeatureFileViewModel, string>(x => x.FileName)
            .ThenBy<FeatureFileViewModel, string>(x => x.Name).ToList();

        return await WriteSettings(settings);
    }

    public static async Task<FeatureIntersectTaggerSettings> SetPadUsAttributes(
        List<string> attributes)
    {
        var settings = await ReadSettings();

        settings.PadUsAttributes = attributes;

        return await WriteSettings(settings);
    }

    public static async Task<FeatureIntersectTaggerSettings> SetPadUsDirectory(string directory)
    {
        var settings = await ReadSettings();

        if (string.IsNullOrEmpty(directory)) directory = string.Empty;

        settings.PadUsDirectory = directory;

        return await WriteSettings(settings);
    }

    public static async Task<FeatureIntersectTaggerSettings> WriteSettings(
        FeatureIntersectTaggerSettings setting)
    {
        var settingsFile = await DefaultSettingsFile();
        var serializedSettings = JsonSerializer.Serialize(setting, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
        return setting;
    }
}