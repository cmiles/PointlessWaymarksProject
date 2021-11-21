using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.ColumnSort;

[ObservableObject]
public partial class ColumnSortControlSortItem
{
    [ObservableProperty] private string _columnName;
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private ListSortDirection _defaultSortDirection = ListSortDirection.Ascending;
    [ObservableProperty] private int _order;
    [ObservableProperty] private ListSortDirection _sortDirection = ListSortDirection.Ascending;
}