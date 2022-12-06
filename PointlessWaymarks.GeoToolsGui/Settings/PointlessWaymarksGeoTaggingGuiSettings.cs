namespace PointlessWaymarks.GeoToolsGui.Settings;

public class PointlessWaymarksGeoTaggingGuiSettings
{
    public bool CreateBackups { get; set; } = true;
    public bool CreateBackupsInDefaultStorage { get; set; } = true;
    public string ExifToolFullName { get; set; } = string.Empty;
    public string FilesToTagLastDirectoryFullName { get; set; } = string.Empty;
    public string GpxLastDirectoryFullName { get; set; } = string.Empty;
    public bool OverwriteExistingGeoLocation { get; set; }
    public int PointsMustBeWithinMinutes { get; set; } = 10;
    public bool ReplaceExistingFiles { get; set; }
    public bool TestRunOnly { get; set; }
}