using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.GeoToolsGui;

public static class GeoToolsGuiSettingTools
{
    public static GeoToolsGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "SiteViewerGuiSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new GeoToolsGuiSettings()));

            return new GeoToolsGuiSettings();
        }

        return JsonSerializer.Deserialize<GeoToolsGuiSettings>(FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new GeoToolsGuiSettings();
    }

    public static async Task WriteSettings(GeoToolsGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwGeoToolsSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        var serializedNewSettings = JsonSerializer.Serialize(settings);
        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, serializedNewSettings);
        await stream.DisposeAsync();
    }
}