using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.ColumnSort;

public partial class ColumnSortControlSortItem : ObservableObject
{
    [ObservableProperty] private string _columnName;
    [ObservableProperty] private ListSortDirection _defaultSortDirection = ListSortDirection.Ascending;
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private int _order;
    [ObservableProperty] private ListSortDirection _sortDirection = ListSortDirection.Ascending;

    public ColumnSortControlSortItem()
    {
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(DefaultSortDirection))
            SortDirection = DefaultSortDirection;
    }
}