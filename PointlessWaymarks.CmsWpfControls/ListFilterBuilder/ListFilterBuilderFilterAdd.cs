using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

[NotifyPropertyChanged]
public partial class ListFilterBuilderFilterAdd
{
    public required RelayCommand? AddFilterCommand { get; set; }
    public required List<string> AppliesTo { get; set; }
    public required string Description { get; set; }
}