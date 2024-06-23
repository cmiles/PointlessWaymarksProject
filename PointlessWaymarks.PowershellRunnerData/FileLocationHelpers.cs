using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.PowerShellRunnerData;

public static class FileLocationHelpers
{
    public static DirectoryInfo DefaultStorageDirectory()
    {
        var directory =
            new DirectoryInfo(Path.Combine(FileLocationTools.DefaultStorageDirectory().FullName,
                "Powershell Run and Record"));

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