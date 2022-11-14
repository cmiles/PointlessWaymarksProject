using System.IO;
using System.Text.Json;

namespace PointlessWaymarks.GeoTaggingGui;

public static class SettingTools
{
    public static async Task<FileInfo> DefaultSettingsFile()
    {
        var settingsFile =
            new FileInfo(Path.Combine(DefaultStorageDirectory().FullName, "PwGeoTaggingGuiSettings.json"));

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

    /// <summary>
    ///     This returns the default Pointless Waymarks storage directory - currently in the users
    ///     My Documents in a Pointless Waymarks Cms Folder - this will return the same value regardless
    ///     of settings, site locations, etc...
    /// </summary>
    /// <returns></returns>
    public static DirectoryInfo DefaultStorageDirectory()
    {
        var directory =
            new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Pointless Waymarks Cms"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
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