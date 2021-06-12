using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.PostList
{
    public class PostListLoader : ContentListLoaderBase
    {
        public PostListLoader(int? partialLoadQuantity) : base("Posts", partialLoadQuantity)
        {
            DataNotificationTypesToRespondTo = new List<DataNotificationContentType> {DataNotificationContentType.Post};
        }

        public override async Task<List<object>> LoadItems(IProgress<string> progress = null)
        {
            var db = await Db.Context();

            if (PartialLoadQuantity != null)
            {
                progress?.Report($"Loading Post Content from DB - Max {PartialLoadQuantity} Items");
                var returnItems = (await db.PostContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

                AllItemsLoaded = await db.PostContents.CountAsync() <= returnItems.Count;

                return returnItems;
            }

            progress?.Report("Loading All Post Content from DB");

            AllItemsLoaded = true;

            return (await db.PostContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync()).Cast<object>().ToList();
        }
    }
}