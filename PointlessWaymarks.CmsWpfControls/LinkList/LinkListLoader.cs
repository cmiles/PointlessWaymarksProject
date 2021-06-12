using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.LinkList
{
    public class LinkListLoader : ContentListLoaderBase
    {
        public LinkListLoader(int? partialLoadQuantity) : base("Links", partialLoadQuantity)
        {
            DataNotificationTypesToRespondTo = new List<DataNotificationContentType> {DataNotificationContentType.Link};
        }

        public override async Task<List<object>> LoadItems(IProgress<string> progress = null)
        {
            var db = await Db.Context();

            if (PartialLoadQuantity != null)
            {
                progress?.Report($"Loading Link Content from DB - Max {PartialLoadQuantity} Items");
                var returnItems = (await db.LinkContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

                AllItemsLoaded = await db.LinkContents.CountAsync() <= returnItems.Count;

                return returnItems;
            }

            progress?.Report("Loading All Link Content from DB");

            AllItemsLoaded = true;

            return (await db.LinkContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync()).Cast<object>().ToList();
        }
    }
}