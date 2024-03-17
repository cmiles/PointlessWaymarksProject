using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

[NotifyPropertyChanged]
public partial class TextSearchFieldBuilder
{
    public required string FieldTitle { get; set; }
    public bool Not { get; set; }
    public string SearchText { get; set; } = string.Empty;
}