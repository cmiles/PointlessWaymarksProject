using System.IO;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.WpfCommon.FileList;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class FileBasedGeoTaggerFilesToTagSettings(FileBasedGeoTaggerContext context) : IFileListSettings
{
    public Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = context.Settings.FilesToTagLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return Task.FromResult<DirectoryInfo?>(null);

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return Task.FromResult<DirectoryInfo?>(null);

        return Task.FromResult(returnDirectory)!;
    }

    public Task SetLastDirectory(string newDirectory)
    {
        context.Settings.FilesToTagLastDirectoryFullName = newDirectory;
        return Task.CompletedTask;
    }
}