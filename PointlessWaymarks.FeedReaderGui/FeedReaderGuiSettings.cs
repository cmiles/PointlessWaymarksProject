using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderGui;

[NotifyPropertyChanged]
public partial class FeedReaderGuiSettings
{
    public string LastDatabaseFile { get; set; } = string.Empty;
    public string? LastDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"M:\PointlessWaymarksPublications";
    public bool AutoMarkReadDefault { get; set; } = true;
}