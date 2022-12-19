#region

using System.IO;
using System.Text.Json;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CommonTools;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class ConnectBasedGeoTaggerSettingTools
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
                JsonSerializer.Serialize(blankSettings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
            settingsFile.Refresh();
        }

        return settingsFile;
    }

    public static async Task<ConnectBasedGeoTaggerSettings> ReadSettings()
    {
        var json = FileLoadTools.ReadAllText((await DefaultSettingsFile()).FullName);
        return JsonSerializer.Deserialize<ConnectBasedGeoTaggerSettings>(json) ??
               new ConnectBasedGeoTaggerSettings();
    }

    public static async Task<ConnectBasedGeoTaggerSettings> WriteSettings(
        ConnectBasedGeoTaggerSettings setting)
    {
        var settingsFile = await DefaultSettingsFile();
        var serializedSettings = JsonSerializer.Serialize(setting, new JsonSerializerOptions { WriteIndented = true });
        SettingsWriteQueue.Enqueue(async () =>
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings));
        return setting;
    }
}