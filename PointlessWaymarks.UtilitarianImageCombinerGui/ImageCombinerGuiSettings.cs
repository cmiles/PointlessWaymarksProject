using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.UtilitarianImageCombinerGui;

[NotifyPropertyChanged]
public partial class ImageCombinerGuiSettings
{
    public string? LastFileSourceDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"M:\PointlessWaymarksPublications";
    public string SaveToDirectory { get; set; } = string.Empty;
}