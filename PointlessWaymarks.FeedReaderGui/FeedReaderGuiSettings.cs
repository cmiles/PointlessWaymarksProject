using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderGui;

[NotifyPropertyChanged]
public partial class FeedReaderGuiSettings
{
    public string LastDatabaseFile { get; set; } = string.Empty;
    public string? LastDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"https://software.pointlesswaymarks.com/Software/PointlessWaymarksSoftwareList.json";
    public bool AutoMarkReadDefault { get; set; } = true;
}