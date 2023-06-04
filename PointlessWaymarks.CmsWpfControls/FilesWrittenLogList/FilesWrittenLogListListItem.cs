#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.FilesWrittenLogList;

[NotifyPropertyChanged]
public partial class FilesWrittenLogListListItem
{
    public string FileBase { get; set; } = string.Empty;
    public bool IsInGenerationDirectory { get; set; }
    public string TransformedFile { get; set; } = string.Empty;
    public string WrittenFile { get; set; } = string.Empty;
    public DateTime WrittenOn { get; set; }
}