using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ColumnSort;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.PhotoList;

public class PhotoListLoader : ContentListLoaderBase, IContentListLoader
{
    public PhotoListLoader(int? partialLoadQuantity) : base("Photos", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = new List<DataNotificationContentType>
        {
            DataNotificationContentType.Photo
        };
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading Photo Content from DB - Max {PartialLoadQuantity} Items");
            var returnItems = (await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

            AllItemsLoaded = await db.PhotoContents.CountAsync() <= returnItems.Count;

            return returnItems;
        }

        progress?.Report("Loading All Photo Content from DB");

        AllItemsLoaded = true;

        return (await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }

    public ColumnSortControlContext SortContext()
    {
        return SortContextPhotoDefault();
    }

    public static ColumnSortControlContext SortContextPhotoDefault()
    {
        return new()
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
                    DisplayName = "Photo Date",
                    ColumnName = "DbEntry.PhotoCreatedOn",
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