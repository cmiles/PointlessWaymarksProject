using System.Collections.ObjectModel;

namespace PointlessWaymarks.WpfCommon;

public interface IStandardListWithContext<T> : IListSelectionWithContext<T>
{
    ObservableCollection<T> Items { get; init; }
}