using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Commands;

namespace PointlessWaymarks.CmsWpfControls.ColumnSort;

[ObservableObject]
public partial class ColumnSortControlContext
{
    [ObservableProperty] private Command<ColumnSortControlSortItem> _columnSortAddCommand;
    [ObservableProperty] private Command<ColumnSortControlSortItem> _columnSortToggleCommand;
    [ObservableProperty] private List<ColumnSortControlSortItem> _items;

    public ColumnSortControlContext()
    {
        ColumnSortToggleCommand = new Command<ColumnSortControlSortItem>(x =>
        {
            ToggleItem(x);
            SortUpdated?.Invoke(this, SortDescriptions());
        });

        ColumnSortAddCommand = new Command<ColumnSortControlSortItem>(x =>
        {
            AddItem(x);
            SortUpdated?.Invoke(this, SortDescriptions());
        });
    }

    public EventHandler<List<SortDescription>> SortUpdated { get; set; }

    private void AddItem(ColumnSortControlSortItem sortItem)
    {
        var currentSortCount = Items.Count(x => x.Order > 0);
        if (!Items.Any(x => x.Order > 0) || currentSortCount == 1 && sortItem.Order > 0)
        {
            ToggleItem(sortItem);
            return;
        }

        switch (sortItem.Order)
        {
            case > 0 when sortItem.SortDirection != sortItem.DefaultSortDirection:
                sortItem.SortDirection = sortItem.DefaultSortDirection;
                sortItem.Order = 0;
                OrderSorts();
                return;
            case > 0:
                sortItem.SortDirection = ChangeSortDirection(sortItem.SortDirection);
                return;
            default:
                sortItem.SortDirection = sortItem.DefaultSortDirection;
                sortItem.Order = Items.Max(y => y.Order) + 1;
                break;
        }
    }

    private ListSortDirection ChangeSortDirection(ListSortDirection currentDirection)
    {
        return currentDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
    }

    private void OrderSorts()
    {
        var newOrder = 1;
        Items.Where(x => x.Order > 0).OrderBy(x => x.Order).ToList().ForEach(x => x.Order = newOrder++);
    }

    public List<SortDescription> SortDescriptions()
    {
        var returnList = new List<SortDescription>();

        foreach (var loopSorts in Items.Where(x => x.Order > 0).OrderBy(x => x.Order).ToList())
            returnList.Add(new SortDescription(loopSorts.ColumnName, loopSorts.SortDirection));

        return returnList;
    }

    private void ToggleItem(ColumnSortControlSortItem x)
    {
        if (x.Order == 1)
        {
            x.SortDirection = ChangeSortDirection(x.SortDirection);
            return;
        }

        x.SortDirection = x.DefaultSortDirection;
        x.Order = 1;

        Items.Where(y => y != x).ToList().ForEach(y =>
        {
            y.Order = 0;
            y.SortDirection = x.DefaultSortDirection;
        });
    }
}