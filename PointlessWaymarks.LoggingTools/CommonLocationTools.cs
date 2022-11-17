namespace PointlessWaymarks.LoggingTools;

public static class CommonLocationTools
{
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
    ///     This returns the default Pointless Waymarks storage directory - currently in the users
    ///     My Documents in a Pointless Waymarks Cms Folder - this will return the same value regardless
    ///     of settings, site locations, etc...
    /// </summary>
    /// <returns></returns>
    public static DirectoryInfo DefaultStorageDirectory()
    {
        var directory =
            new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Pointless Waymarks Cms"));

        if (!directory.Exists) directory.Create();

        directory.Refresh();

        return directory;
    }
}