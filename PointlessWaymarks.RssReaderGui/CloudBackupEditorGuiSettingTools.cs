using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.RssReaderGui;

public static class RssReaderGuiSettingTools
{
    public static RssReaderGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwRssReaderSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new RssReaderGuiSettings()));

            return new RssReaderGuiSettings();
        }

        return JsonSerializer.Deserialize<RssReaderGuiSettings>(FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new RssReaderGuiSettings();
    }

    public static async Task WriteSettings(RssReaderGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwRssReaderSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, settings);
        await stream.DisposeAsync();
    }
}