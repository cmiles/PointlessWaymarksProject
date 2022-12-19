#region

using System.IO;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.WpfCommon.FileList;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class FileBasedGeoTaggerFilesToTagSettings : IFileListSettings
{
    private readonly FileBasedGeoTaggerContext _context;

    public FileBasedGeoTaggerFilesToTagSettings(FileBasedGeoTaggerContext context)
    {
        _context = context;
    }

    public async Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = _context.Settings.FilesToTagLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async System.Threading.Tasks.Task SetLastDirectory(string newDirectory)
    {
        _context.Settings.FilesToTagLastDirectoryFullName = newDirectory ?? string.Empty;
    }
}