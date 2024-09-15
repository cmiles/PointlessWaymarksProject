using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.UtilitarianImageCombinerGui;

[NotifyPropertyChanged]
public partial class ImageCombinerGuiSettings
{
    public string? LastFileSourceDirectory { get; set; } = string.Empty;
    public string ProgramUpdateDirectory { get; set; } = @"https://software.pointlesswaymarks.com/Software/PointlessWaymarksSoftwareList.json";
    public string SaveToDirectory { get; set; } = string.Empty;
}