using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.RssReaderGui;

[NotifyPropertyChanged]
public partial class RssReaderGuiSettings
{
    public string DatabaseFile { get; set; } = string.Empty;
    public string LastDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"M:\PointlessWaymarksPublications";
}