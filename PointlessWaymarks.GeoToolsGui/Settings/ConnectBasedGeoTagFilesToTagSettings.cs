#region

using System.IO;
using PointlessWaymarks.WpfCommon.FileList;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class ConnectBasedGeoTagFilesToTagSettings : IFileListSettings
{
    public async Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = (await ConnectBasedGeoTaggerSettingTools.ReadSettings()).FilesToTagLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async System.Threading.Tasks.Task SetLastDirectory(string newDirectory)
    {
        var settings = await ConnectBasedGeoTaggerSettingTools.ReadSettings();
        settings.FilesToTagLastDirectoryFullName = newDirectory ?? string.Empty;
        await ConnectBasedGeoTaggerSettingTools.WriteSettings(settings);
    }
}