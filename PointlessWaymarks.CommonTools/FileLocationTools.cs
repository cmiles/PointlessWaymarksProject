namespace PointlessWaymarks.CommonTools;

public static class FileLocationTools
{
    /// <summary>
    ///     This returns the default Pointless Waymarks storage directory - currently in the users
    ///     My Documents in a Pointless Waymarks Cms Folder - this will return the same value regardless
    ///     of settings, site locations, etc...
    /// </summary>
    /// <returns></returns>
    public static DirectoryInfo DefaultAssetsStorageDirectory()
    {
        var directory =
            new DirectoryInfo(Path.Combine(DefaultStorageDirectory().FullName,
                "Assets"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo DefaultErrorReportsDirectory()
    {
        var baseDirectory = DefaultStorageDirectory();

        var directory =
            new DirectoryInfo(Path.Combine(baseDirectory.FullName, "Error-Reports"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    /// <summary>
    ///     This returns the default Pointless Waymarks storage directory - currently in the users
    ///     My Documents in a Pointless Waymarks Cms Folder - this will return the same value regardless
    ///     of settings, site locations, etc...
    /// </summary>
    /// <returns></returns>
    public static DirectoryInfo DefaultLogStorageDirectory()
    {
        var baseDirectory = DefaultStorageDirectory();

        var directory = new DirectoryInfo(Path.Combine(baseDirectory.FullName, "PwLogs"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    /// <summary>
    ///     This returns the default Pointless Waymarks Project storage directory - currently in the users
    ///     My Documents in a Pointless Waymarks Project Folder - this will return the same value regardless
    ///     of settings, site locations, etc...
    /// </summary>
    /// <returns></returns>
    public static DirectoryInfo DefaultStorageDirectory()
    {
        var directory =
            new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Pointless Waymarks Project"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo TempStorageDirectory()
    {
        var directory = new DirectoryInfo(Path.Combine(
            DefaultStorageDirectory().FullName, 
            "TemporaryFiles"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }

    public static DirectoryInfo TempStorageHtmlDirectory()
    {
        var directory = new DirectoryInfo(Path.Combine(DefaultStorageDirectory().FullName, "LocalHtml"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }
}