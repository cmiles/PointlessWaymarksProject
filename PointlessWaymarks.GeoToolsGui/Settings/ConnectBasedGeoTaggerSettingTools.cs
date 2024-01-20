using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public static class ConnectBasedGeoTaggerSettingTools
{
    private static readonly TaskQueue SettingsWriteQueue = new();

    public static async Task<FileInfo> DefaultSettingsFile()
    {
        var settingsFile =
            new FileInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                "PwGtgConnectBasedGeoTaggerSettings.json"));

        if (!settingsFile.Exists)
        {
            var blankSettings = new ConnectBasedGeoTaggerSettings();
            var serializedSettings =
                JsonSerializer.Serialize(blankSettings, JsonTools.WriteIndentedOptions);
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
            settingsFile.Refresh();
        }

        return settingsFile;
    }

    public static async Task<ConnectBasedGeoTaggerSettings> ReadSettings()
    {
        var json = FileAndFolderTools.ReadAllText((await DefaultSettingsFile()).FullName);
        return JsonSerializer.Deserialize<ConnectBasedGeoTaggerSettings>(json) ??
               new ConnectBasedGeoTaggerSettings();
    }

    public static async Task WriteSettings(ConnectBasedGeoTaggerSettings setting)
    {
        var settingsFile = await DefaultSettingsFile();
        var serializedSettings = JsonSerializer.Serialize(setting, JsonTools.WriteIndentedOptions);
        SettingsWriteQueue.Enqueue(async () =>
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings));
    }
}