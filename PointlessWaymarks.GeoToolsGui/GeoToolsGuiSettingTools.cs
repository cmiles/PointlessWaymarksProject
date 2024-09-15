using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.GeoToolsGui;

public static class GeoToolsGuiSettingTools
{
    public static GeoToolsGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwGeoToolsSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists)
            return JsonSerializer.Deserialize<GeoToolsGuiSettings>(FileAndFolderTools.ReadAllText(settingsFileName)) ??
                   new GeoToolsGuiSettings();
        
        File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new GeoToolsGuiSettings()));

        return new GeoToolsGuiSettings();

    }

    public static async Task WriteSettings(GeoToolsGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwGeoToolsSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, settings);
        await stream.DisposeAsync();
    }
}