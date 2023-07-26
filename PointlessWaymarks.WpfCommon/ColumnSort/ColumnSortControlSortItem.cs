using System.ComponentModel;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.ColumnSort;

[NotifyPropertyChanged]
public partial class ColumnSortControlSortItem
{
    public ColumnSortControlSortItem()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public string ColumnName { get; set; } = string.Empty;
    public ListSortDirection DefaultSortDirection { get; set; } = ListSortDirection.Ascending;
    public string DisplayName { get; set; } = string.Empty;
    public int Order { get; set; }
    public ListSortDirection SortDirection { get; set; } = ListSortDirection.Ascending;

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(DefaultSortDirection))
            SortDirection = DefaultSortDirection;
    }
}