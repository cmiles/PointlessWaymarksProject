using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.FeedReaderGui;

public static class FeedReaderGuiSettingTools
{
    public static FeedReaderGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwFeedReaderSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new FeedReaderGuiSettings()));

            return new FeedReaderGuiSettings();
        }

        return JsonSerializer.Deserialize<FeedReaderGuiSettings>(FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new FeedReaderGuiSettings();
    }

    public static async Task WriteSettings(FeedReaderGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwFeedReaderSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, settings);
        await stream.DisposeAsync();
    }
}