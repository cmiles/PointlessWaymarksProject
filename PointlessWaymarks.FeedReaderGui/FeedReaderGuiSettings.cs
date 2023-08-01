using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderGui;

[NotifyPropertyChanged]
public partial class FeedReaderGuiSettings
{
    public string DatabaseFile { get; set; } = string.Empty;
    public string LastDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"M:\PointlessWaymarksPublications";
}