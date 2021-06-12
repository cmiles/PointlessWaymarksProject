using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.ColumnSort;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public interface IContentListLoader : INotifyPropertyChanged
    {
        bool AddNewItemsFromDataNotifications { get; set; }
        bool AllItemsLoaded { get; set; }
        List<DataNotificationContentType> DataNotificationTypesToRespondTo { get; set; }
        string ListHeaderName { get; }
        int? PartialLoadQuantity { get; set; }
        bool ShowType { get; set; }
        Task<List<object>> LoadItems(IProgress<string> progress = null);

        ColumnSortControlContext SortContext()
        {
            return ContentListLoaderBase.SortContextDefault();
        }
    }
}