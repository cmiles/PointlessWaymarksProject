using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ColumnSort;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.PhotoList
{
    public class PhotoListLoader : ContentListLoaderBase, IContentListLoader
    {
        public PhotoListLoader(int? partialLoadQuantity) : base("Photos", partialLoadQuantity)
        {
            DataNotificationTypesToRespondTo = new List<DataNotificationContentType>
            {
                DataNotificationContentType.Photo
            };
        }

        public override async Task<bool> CheckAllItemsAreLoaded()
        {
            if (PartialLoadQuantity == null) return true;

            var db = await Db.Context();

            return !(await db.PhotoContents.CountAsync() > PartialLoadQuantity);
        }

        public override async Task<List<object>> LoadItems(IProgress<string> progress = null)
        {
            var listItems = new List<object>();

            var db = await Db.Context();

            if (PartialLoadQuantity != null)
            {
                progress?.Report($"Loading Photo Content from DB - Max {PartialLoadQuantity} Items");
                listItems.AddRange(await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync());

                AllItemsLoaded = await CheckAllItemsAreLoaded();

                return listItems;
            }

            progress?.Report("Loading Photo Content from DB");
            listItems.AddRange(await db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync());

            return listItems;
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
}