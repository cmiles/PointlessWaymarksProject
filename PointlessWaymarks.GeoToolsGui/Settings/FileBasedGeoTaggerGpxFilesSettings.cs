using System.IO;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.WpfCommon.FileList;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class FileBasedGeoTaggerGpxFilesSettings : IFileListSettings
{
    private readonly FileBasedGeoTaggerContext _context;

    public FileBasedGeoTaggerGpxFilesSettings(FileBasedGeoTaggerContext context)
    {
        _context = context;
    }

    public Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = _context.Settings.GpxLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return Task.FromResult<DirectoryInfo?>(null);

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return Task.FromResult<DirectoryInfo?>(null);

        return Task.FromResult(returnDirectory)!;
    }

    public Task SetLastDirectory(string newDirectory)
    {
        _context.Settings.GpxLastDirectoryFullName = newDirectory;
        return Task.CompletedTask;
    }
}