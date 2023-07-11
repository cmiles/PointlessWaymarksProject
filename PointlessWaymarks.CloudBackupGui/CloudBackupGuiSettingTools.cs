using System.IO;
using System.Text.Json;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupGui;

public static class CloudBackupGuiSettingTools
{
    public static CloudBackupGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwCloudBackupSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new CloudBackupGuiSettings()));

            return new CloudBackupGuiSettings();
        }

        return JsonSerializer.Deserialize<CloudBackupGuiSettings>(FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new CloudBackupGuiSettings();
    }

    public static async Task WriteSettings(CloudBackupGuiSettings settings)
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