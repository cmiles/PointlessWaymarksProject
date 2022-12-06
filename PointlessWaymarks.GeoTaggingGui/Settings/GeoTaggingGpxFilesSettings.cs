using System.IO;
using PointlessWaymarks.WpfCommon.FileList;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class GeoTaggingGpxFilesSettings : IFileListSettings
{
    public async Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = (await GeoTaggingGuiSettingTools.ReadSettings()).GpxLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async System.Threading.Tasks.Task SetLastDirectory(string newDirectory)
    {
        var settings = await GeoTaggingGuiSettingTools.ReadSettings();
        settings.GpxLastDirectoryFullName = newDirectory ?? string.Empty;
        await GeoTaggingGuiSettingTools.WriteSettings(settings);
    }
}