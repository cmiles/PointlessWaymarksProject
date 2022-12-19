#region

using System.IO;
using PointlessWaymarks.WpfCommon.FileList;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class FileBasedGeoTaggerGpxFilesSettings : IFileListSettings
{
    public async Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = (await FileBasedGeoTaggerSettingTools.ReadSettings()).GpxLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async System.Threading.Tasks.Task SetLastDirectory(string newDirectory)
    {
        var settings = await FileBasedGeoTaggerSettingTools.ReadSettings();
        settings.GpxLastDirectoryFullName = newDirectory ?? string.Empty;
        await FileBasedGeoTaggerSettingTools.WriteSettings(settings);
    }
}