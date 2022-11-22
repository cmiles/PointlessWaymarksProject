using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.GeoTaggingGui;

public static class GeoTaggingGuiSettingTools
{
    public static async Task<FileInfo> DefaultSettingsFile()
    {
        var settingsFile =
            new FileInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName, "PwGeoTaggingGuiSettings.json"));

        if (!settingsFile.Exists)
        {
            var blankSettings = new PointlessWaymarksGeoTaggingGuiSettings();
            var serializedSettings =
                JsonSerializer.Serialize(blankSettings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
            settingsFile.Refresh();
        }

        return settingsFile;
    }

    public static async Task<PointlessWaymarksGeoTaggingGuiSettings> ReadSettings()
    {
        PointlessWaymarksGeoTaggingGuiSettings? settings;

        return JsonSerializer.Deserialize<PointlessWaymarksGeoTaggingGuiSettings>(
                   await File.ReadAllTextAsync((await DefaultSettingsFile()).FullName)) ??
               new PointlessWaymarksGeoTaggingGuiSettings();
    }

    public static async Task<PointlessWaymarksGeoTaggingGuiSettings> WriteSettings(
        PointlessWaymarksGeoTaggingGuiSettings setting)
    {
        var settingsFile = await DefaultSettingsFile();
        var serializedSettings = JsonSerializer.Serialize(setting, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
        return setting;
    }
}