using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.RssReaderData;

public static class FileLocationHelpers
{
    public static DirectoryInfo DefaultStorageDirectory()
    {
        var directory =
            new DirectoryInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                "Rss"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }
}