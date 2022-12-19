#region

using PointlessWaymarks.GeoToolsGui.Models;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class FeatureIntersectTaggerSettings
{
    public bool CreateBackups { get; set; }
    public bool CreateBackupsInDefaultStorage { get; set; }
    public string ExifToolFullName { get; set; } = string.Empty;
    public List<FeatureFileViewModel> FeatureIntersectFiles { get; set; } = new();
    public string FilesToTagLastDirectoryFullName { get; set; } = string.Empty;
    public List<string> PadUsAttributes { get; set; } = new();
    public string PadUsDirectory { get; set; } = string.Empty;
    public bool SanitizeTags { get; set; } = true;
    public bool TagSpacesToHyphens { get; set; }
    public bool TagsToLowerCase { get; set; } = true;
    public bool TestRunOnly { get; set; }
}