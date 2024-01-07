using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.ColumnSort;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public abstract class ContentListLoaderBase : IContentListLoader
{
    private bool _addNewItemsFromDataNotifications = true;
    private bool _allItemsLoaded;

    private List<DataNotificationContentType> _dataNotificationTypesToRespondTo = [];

    private int? _partialLoadQuantity;

    private bool _showType;

    public ContentListLoaderBase(string headerName, int? partialLoadQuantity)
    {
        PartialLoadQuantity = partialLoadQuantity;
        ListHeaderName = headerName;
    }

    public bool AddNewItemsFromDataNotifications
    {
        get => _addNewItemsFromDataNotifications;
        set
        {
            if (value == _addNewItemsFromDataNotifications) return;
            _addNewItemsFromDataNotifications = value;
            OnPropertyChanged();
        }
    }

    public bool AllItemsLoaded
    {
        get => _allItemsLoaded;
        set
        {
            if (value == _allItemsLoaded) return;
            _allItemsLoaded = value;
            OnPropertyChanged();
        }
    }

    public List<DataNotificationContentType> DataNotificationTypesToRespondTo
    {
        get => _dataNotificationTypesToRespondTo;
        set
        {
            if (Equals(value, _dataNotificationTypesToRespondTo)) return;
            _dataNotificationTypesToRespondTo = value;
            OnPropertyChanged();
        }
    }

    public string ListHeaderName { get; }

    public int? PartialLoadQuantity
    {
        get => _partialLoadQuantity;
        set
        {
            if (value == _partialLoadQuantity) return;
            _partialLoadQuantity = value;
            OnPropertyChanged();
        }
    }

    public bool ShowType
    {
        get => _showType;
        set
        {
            if (value == _showType) return;
            _showType = value;
            OnPropertyChanged();
        }
    }

    public abstract Task<List<object>> LoadItems(IProgress<string>? progress = null);

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static ColumnSortControlContext SortContextDefault()
    {
        return new ColumnSortControlContext
        {
            Items =
            [
                new()
                {
                    DisplayName = "Updated",
                    ColumnName = "DbEntry.LatestUpdate",
                    Order = 1,
                    DefaultSortDirection = ListSortDirection.Descending
                },

                new()
                {
                    DisplayName = "Created",
                    ColumnName = "DbEntry.CreatedOn",
                    DefaultSortDirection = ListSortDirection.Descending
                },

                new()
                {
                    DisplayName = "Title",
                    ColumnName = "DbEntry.Title",
                    DefaultSortDirection = ListSortDirection.Ascending
                }
            ]
        };
    }
}