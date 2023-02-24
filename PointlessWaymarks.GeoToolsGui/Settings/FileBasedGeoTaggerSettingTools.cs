using System.IO;
using System.Text.Json;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public static class FileBasedGeoTaggerSettingTools
{
    private static readonly TaskQueue SettingsWriteQueue = new();

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
        var json = FileAndFolderTools.ReadAllText((await DefaultSettingsFile()).FullName);
        return JsonSerializer.Deserialize<FileBasedGeoTaggerSettings>(json) ??
               new FileBasedGeoTaggerSettings();
    }

    public static async Task WriteSettings(FileBasedGeoTaggerSettings setting)
    {
        var settingsFile = await DefaultSettingsFile();
        var serializedSettings = JsonSerializer.Serialize(setting, new JsonSerializerOptions { WriteIndented = true });
        SettingsWriteQueue.Enqueue(async () => await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings));
    }
}