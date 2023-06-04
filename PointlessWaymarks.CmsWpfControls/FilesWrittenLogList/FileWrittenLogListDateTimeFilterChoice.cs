using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.FilesWrittenLogList;

[NotifyPropertyChanged]
public partial class FileWrittenLogListDateTimeFilterChoice
{
    public string DisplayText { get; set; } = string.Empty;
    public DateTime? FilterDateTimeUtc { get; set; }
}