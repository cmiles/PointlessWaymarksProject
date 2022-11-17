using System.IO;
using System.Text.Json;
using PointlessWaymarks.LoggingTools;

namespace PointlessWaymarks.FeatureIntersectionTaggingGui;

public static class FeatureIntersectionGuiSettingTools
{
    public static async Task<FileInfo> DefaultSettingsFile()
    {
        var settingsFile =
            new FileInfo(Path.Combine(CommonLocationTools.DefaultStorageDirectory().FullName,
                "PwFeatureIntersectionGuiSettings.json"));

        if (!settingsFile.Exists)
        {
            var blankSettings = new PointlessWaymarksFeatureIntersectionGuiSettings();
            var serializedSettings =
                JsonSerializer.Serialize(blankSettings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
            settingsFile.Refresh();
        }

        return settingsFile;
    }

    public static async Task<PointlessWaymarksFeatureIntersectionGuiSettings> ReadSettings()
    {
        PointlessWaymarksFeatureIntersectionGuiSettings? settings;

        return JsonSerializer.Deserialize<PointlessWaymarksFeatureIntersectionGuiSettings>(
                   await File.ReadAllTextAsync((await DefaultSettingsFile()).FullName)) ??
               new PointlessWaymarksFeatureIntersectionGuiSettings();
    }

    public static async Task<PointlessWaymarksFeatureIntersectionGuiSettings> WriteSettings(
        PointlessWaymarksFeatureIntersectionGuiSettings setting)
    {
        var settingsFile = await DefaultSettingsFile();
        var serializedSettings = JsonSerializer.Serialize(setting, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
        return setting;
    }
}