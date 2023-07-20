using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupEditorGui;

public static class CloudBackupEditorGuiSettingTools
{
    public static CloudBackupEditorGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwCloudBackupSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new CloudBackupEditorGuiSettings()));

            return new CloudBackupEditorGuiSettings();
        }

        return JsonSerializer.Deserialize<CloudBackupEditorGuiSettings>(FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new CloudBackupEditorGuiSettings();
    }

    public static async Task WriteSettings(CloudBackupEditorGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwCloudBackupSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, settings);
        await stream.DisposeAsync();
    }
}