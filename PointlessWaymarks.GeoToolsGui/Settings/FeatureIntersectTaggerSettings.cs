using System.Collections.ObjectModel;
using PointlessWaymarks.GeoToolsGui.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.GeoToolsGui.Settings;

[NotifyPropertyChanged]
public partial class FeatureIntersectTaggerSettings
{
    public bool CreateBackups { get; set; }
    public bool CreateBackupsInDefaultStorage { get; set; }
    public string ExifToolFullName { get; set; } = string.Empty;
    public ObservableCollection<FeatureFileContext> FeatureIntersectFiles { get; set; } = new();
    public string FilesToTagLastDirectoryFullName { get; set; } = string.Empty;
    public ObservableCollection<string> PadUsAttributes { get; set; } = new();
    public string PadUsDirectory { get; set; } = string.Empty;
    public bool SanitizeTags { get; set; } = true;
    public bool TagSpacesToHyphens { get; set; }
    public bool TagsToLowerCase { get; set; } = true;
}