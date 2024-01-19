using System.IO;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.WpfCommon.FileList;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class FileBasedGeoTaggerGpxFilesSettings(FileBasedGeoTaggerContext context) : IFileListSettings
{
    public Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = context.Settings.GpxLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return Task.FromResult<DirectoryInfo?>(null);

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return Task.FromResult<DirectoryInfo?>(null);

        return Task.FromResult(returnDirectory)!;
    }

    public Task SetLastDirectory(string newDirectory)
    {
        context.Settings.GpxLastDirectoryFullName = newDirectory;
        return Task.CompletedTask;
    }
}