#region

using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

public static class FileBasedGeoTaggerSettingTools
{
    public static async Task<FileInfo> DefaultSettingsFile()
    {
        var settingsFile =
            new FileInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                "PwGtgFileBasedGeoTaggerSettings.json"));

        if (!settingsFile.Exists)
        {
            var blankSettings = new FileBasedGeoTaggerSettings();
            var serializedSettings =
                JsonSerializer.Serialize(blankSettings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
            settingsFile.Refresh();
        }

        return settingsFile;
    }

    public static async Task<FileBasedGeoTaggerSettings> ReadSettings()
    {
        return JsonSerializer.Deserialize<FileBasedGeoTaggerSettings>(
                   await File.ReadAllTextAsync((await DefaultSettingsFile()).FullName)) ??
               new FileBasedGeoTaggerSettings();
    }

    public static async Task<FileBasedGeoTaggerSettings> WriteSettings(
        FileBasedGeoTaggerSettings setting)
    {
        var settingsFile = await DefaultSettingsFile();
        var serializedSettings = JsonSerializer.Serialize(setting, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
        return setting;
    }
}