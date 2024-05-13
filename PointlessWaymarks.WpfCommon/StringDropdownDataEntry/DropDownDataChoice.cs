using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.DropdownDataEntry;

[NotifyPropertyChanged]
public class DropDownDataChoice
{
    public string DataString { get; set; } = string.Empty;
    public string DisplayString { get; set; } = string.Empty;
}