using System.Diagnostics;
using System.IO;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.WpfCommon.FileList;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class ConnectBasedGeoTagFilesToTagSettings : IFileListSettings
{
    private readonly ConnectBasedGeoTaggerContext _context;

    public ConnectBasedGeoTagFilesToTagSettings(ConnectBasedGeoTaggerContext context)
    {
        _context = context;
    }

    public Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = _context.Settings?.FilesToTagLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return Task.FromResult<DirectoryInfo?>(null);

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return Task.FromResult<DirectoryInfo?>(null);

        return Task.FromResult(returnDirectory)!;
    }

    public Task SetLastDirectory(string newDirectory)
    {
        Debug.Assert(_context.Settings != null, "_context.Settings != null");
        
        _context.Settings.FilesToTagLastDirectoryFullName = newDirectory ?? string.Empty;
        return Task.CompletedTask;
    }
}