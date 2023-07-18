using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CloudBackupData;

public static class FileLocationHelpers
{
    public static DirectoryInfo DefaultStorageDirectory()
    {
        var directory =
            new DirectoryInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                "Cloud Backup"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo ReportsDirectory()
    {
        var directory =
            new DirectoryInfo(Path.Combine(DefaultStorageDirectory().FullName,
                "Reports"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }
}