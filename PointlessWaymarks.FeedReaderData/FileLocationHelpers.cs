using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.FeedReaderData;

public static class FileLocationHelpers
{
    public static DirectoryInfo DefaultStorageDirectory()
    {
        var directory =
            new DirectoryInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                "Feed Reader"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }
}