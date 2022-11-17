using System.IO;
using PointlessWaymarks.WpfCommon.FileList;

namespace PointlessWaymarks.GeoTaggingGui;

public class FilesToTagSettings : IFileListSettings
{
    public async Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = (await SettingTools.ReadSettings()).FilesToTagLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async System.Threading.Tasks.Task SetLastDirectory(string newDirectory)
    {
        var settings = await SettingTools.ReadSettings();
        settings.FilesToTagLastDirectoryFullName = newDirectory ?? string.Empty;
        await SettingTools.WriteSettings(settings);
    }
}