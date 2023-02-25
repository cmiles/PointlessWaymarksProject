using System.IO;

namespace PointlessWaymarks.WpfCommon.FileList;

public interface IFileListSettings
{
    Task<DirectoryInfo?> GetLastDirectory();
    Task SetLastDirectory(string newDirectory);
}