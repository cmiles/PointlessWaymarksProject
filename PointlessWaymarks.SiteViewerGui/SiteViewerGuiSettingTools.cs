using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.SiteViewerGui;

public static class SiteViewerGuiSettingTools
{
    public static SiteViewerGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "SiteViewerGuiSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new SiteViewerGuiSettings()));

            return new SiteViewerGuiSettings();
        }

        return JsonSerializer.Deserialize<SiteViewerGuiSettings>(FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new SiteViewerGuiSettings();
    }

    public static async Task WriteSettings(SiteViewerGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwSiteViewerSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        var serializedNewSettings = JsonSerializer.Serialize(settings);
        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, serializedNewSettings);
        await stream.DisposeAsync();
    }
}