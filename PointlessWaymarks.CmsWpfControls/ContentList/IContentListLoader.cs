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
        int? PartialLoadQuantity { get; set; }
        bool ShowType { get; set; }
        Task<bool> CheckAllItemsAreLoaded();
        Task<List<object>> LoadItems(IProgress<string> progress = null);
        ColumnSortControlContext SortContext()
        {
            return new ColumnSortControlContext
            {
                Items = new List<ColumnSortControlSortItem>
                {
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
                }
            };
        }
    }
}