using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public static class ConnectDownloadSettingTools
{
    private static readonly TaskQueue SettingsWriteQueue = new();

    public static async Task<FileInfo> DefaultSettingsFile()
    {
        var settingsFile =
            new FileInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                "PwGtgConnectDownloadSettings.json"));

        if (!settingsFile.Exists)
        {
            var blankSettings = new ConnectDownloadSettings();
            var serializedSettings =
                JsonSerializer.Serialize(blankSettings, JsonTools.WriteIndentedOptions);
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings);
            settingsFile.Refresh();
        }

        return settingsFile;
    }

    public static async Task<ConnectDownloadSettings> ReadSettings()
    {
        var json = FileAndFolderTools.ReadAllText((await DefaultSettingsFile()).FullName);
        return JsonSerializer.Deserialize<ConnectDownloadSettings>(json) ??
               new ConnectDownloadSettings();
    }

    public static async Task WriteSettings(ConnectDownloadSettings setting)
    {
        var settingsFile = await DefaultSettingsFile();
        var serializedSettings = JsonSerializer.Serialize(setting, JsonTools.WriteIndentedOptions);
        SettingsWriteQueue.Enqueue(async () =>
            await File.WriteAllTextAsync(settingsFile.FullName, serializedSettings));
    }
}