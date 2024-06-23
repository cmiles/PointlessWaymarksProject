using System.IO;
using System.Text.Json;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.PowerShellRunnerGui;

public static class PowerShellRunnerGuiSettingTools
{
    public static PowerShellRunnerGuiSettings ReadSettings()
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwPowerShellRunnerSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (!settingsFile.Exists)
        {
            File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new PowerShellRunnerGuiSettings()));

            return new PowerShellRunnerGuiSettings();
        }

        return JsonSerializer.Deserialize<PowerShellRunnerGuiSettings>(
                   FileAndFolderTools.ReadAllText(settingsFileName)) ??
               new PowerShellRunnerGuiSettings();
    }

    public static async Task WriteSettings(PowerShellRunnerGuiSettings settings)
    {
        var settingsFileName = Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
            "PwPowerShellRunnerSettings.json");
        var settingsFile = new FileInfo(settingsFileName);

        if (settingsFile.Exists) settingsFile.Delete();

        await using var stream = File.Create(settingsFile.FullName);
        await JsonSerializer.SerializeAsync(stream, settings);
    }
}