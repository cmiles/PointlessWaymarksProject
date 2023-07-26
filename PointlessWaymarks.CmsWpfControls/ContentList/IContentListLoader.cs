using System.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.ColumnSort;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public interface IContentListLoader : INotifyPropertyChanged
{
    bool AddNewItemsFromDataNotifications { get; set; }
    bool AllItemsLoaded { get; set; }
    List<DataNotificationContentType> DataNotificationTypesToRespondTo { get; set; }
    string ListHeaderName { get; }
    int? PartialLoadQuantity { get; set; }
    bool ShowType { get; set; }
    Task<List<object>> LoadItems(IProgress<string>? progress = null);

    ColumnSortControlContext SortContext()
    {
        return ContentListLoaderBase.SortContextDefault();
    }
}