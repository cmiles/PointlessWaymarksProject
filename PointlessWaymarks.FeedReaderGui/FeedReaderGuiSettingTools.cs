using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData;

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

    public static DirectoryInfo GetLastDirectory()
    {
        var settings = ReadSettings();
        if (string.IsNullOrWhiteSpace(settings.LastDirectory) || !Directory.Exists(settings.LastDirectory))
            return FileLocationHelpers.DefaultStorageDirectory();

        return new DirectoryInfo(settings.LastDirectory);
    }
    
    public static async Task SetLastDirectory(string lastDirectory)
    {
        if (string.IsNullOrWhiteSpace(lastDirectory) || !Directory.Exists(lastDirectory)) return;
        var settings = ReadSettings();
        settings.LastDirectory = lastDirectory;

        await WriteSettings(settings);
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