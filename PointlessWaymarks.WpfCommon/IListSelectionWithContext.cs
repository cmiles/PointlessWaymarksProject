using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon;

public interface IListSelectionWithContext<T>
{
    StatusControlContext StatusContext { get; set; }
    T? SelectedListItem();
    List<T> SelectedListItems();
}