using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.UtilitarianImageCombinerGui;

public static class ImageCombinerGuiSettingTools
{
    public static ImageCombinerGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwImageStackerSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new ImageCombinerGuiSettings()));

            return new ImageCombinerGuiSettings();
        }

        return JsonSerializer.Deserialize<ImageCombinerGuiSettings>(
                   FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new ImageCombinerGuiSettings();
    }

    public static async Task WriteSettings(ImageCombinerGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwImageStackerSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, settings);
    }

    public static async Task WriteFileSourceDirectory(string fileSourceDirectory)
    {
        var currentSettings = ReadSettings();
        currentSettings.LastFileSourceDirectory = fileSourceDirectory;
        await WriteSettings(currentSettings);
    }

    public static async Task WriteSaveToDirectory(string fileSourceDirectory)
    {
        var currentSettings = ReadSettings();
        currentSettings.SaveToDirectory = fileSourceDirectory;
        await WriteSettings(currentSettings);
    }
}