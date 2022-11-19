namespace PointlessWaymarks.GeoTaggingGui;

public class PointlessWaymarksGeoTaggingGuiSettings
{
    public bool CreateBackups { get; set; } = true;
    public string ExifToolFullName { get; set; } = string.Empty;
    public string FilesToTagLastDirectoryFullName { get; set; } = string.Empty;
    public string GpxLastDirectoryFullName { get; set; } = string.Empty;
    public bool OverwriteExistingGeoLocation { get; set; } = false;
    public int PointsMustBeWithinMinutes { get; set; } = 10;

    public bool TestRunOnly { get; set; } = false;
}