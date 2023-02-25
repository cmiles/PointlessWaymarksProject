using CommunityToolkit.Mvvm.Input;

namespace PointlessWaymarks.WpfCommon.Utility;

public class ContextMenuItemData
{
    public RelayCommand? ItemCommand { get; set; }
    public string ItemName { get; set; } = string.Empty;
}