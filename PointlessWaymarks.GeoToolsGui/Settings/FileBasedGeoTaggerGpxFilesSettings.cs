#region

using System.IO;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.WpfCommon.FileList;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class FileBasedGeoTaggerGpxFilesSettings : IFileListSettings
{
    private readonly FileBasedGeoTaggerContext _context;

    public FileBasedGeoTaggerGpxFilesSettings(FileBasedGeoTaggerContext context)
    {
        _context = context;
    }

    public async Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = _context.Settings.GpxLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async Task SetLastDirectory(string newDirectory)
    {
        _context.Settings.GpxLastDirectoryFullName = newDirectory ?? string.Empty;
    }
}