using PointlessWaymarks.FeatureIntersectionTaggingGui.Models;

namespace PointlessWaymarks.FeatureIntersectionTaggingGui;

public class PointlessWaymarksFeatureIntersectionGuiSettings
{
    public string ExifToolFullName { get; set; } = string.Empty;
    public List<FeatureFileViewModel> FeatureIntersectFiles { get; set; } = new();
    public string FilesToTagLastDirectoryFullName { get; set; } = string.Empty;
    public List<string> PadUsAttributes { get; set; } = new();
    public string PadUsDirectory { get; set; } = string.Empty;
}