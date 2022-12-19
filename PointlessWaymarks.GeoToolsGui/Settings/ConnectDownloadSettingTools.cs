#region

using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class ConnectDownloadSettingTools
{
    public static async Task<FileInfo> DefaultSettingsFile()
    {
        var settingsFile =
            new FileInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                "PwGtgConnectDownloadSettings.json"));

        if (!settingsFile.Exists)
        {
            var blankSettings = new ConnectDownloadSettings();
            var serializedSettings =
                JsonSerializer.Serialize(blankSettings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
            settingsFile.Refresh();
        }

        return settingsFile;
    }

    public static async Task<ConnectDownloadSettings> ReadSettings()
    {
        return JsonSerializer.Deserialize<ConnectDownloadSettings>(
                   await File.ReadAllTextAsync((await DefaultSettingsFile()).FullName)) ??
               new ConnectDownloadSettings();
    }

    public static async Task<ConnectDownloadSettings> WriteSettings(
        ConnectDownloadSettings setting)
    {
        var settingsFile = await DefaultSettingsFile();
        var serializedSettings = JsonSerializer.Serialize(setting, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
        return setting;
    }
}