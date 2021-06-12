using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.ImageList
{
    public class ImageListLoader : ContentListLoaderBase
    {
        public ImageListLoader(int? partialLoadQuantity) : base("Images", partialLoadQuantity)
        {
            DataNotificationTypesToRespondTo = new List<DataNotificationContentType>
            {
                DataNotificationContentType.Image
            };
        }

        public override async Task<List<object>> LoadItems(IProgress<string> progress = null)
        {
            var db = await Db.Context();

            if (PartialLoadQuantity != null)
            {
                progress?.Report($"Loading Image Content from DB - Max {PartialLoadQuantity} Items");
                var returnItems = (await db.ImageContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

                AllItemsLoaded = await db.ImageContents.CountAsync() <= returnItems.Count;

                return returnItems;
            }

            progress?.Report("Loading All Image Content from DB");

            AllItemsLoaded = true;

            return (await db.ImageContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .ToListAsync()).Cast<object>().ToList();
        }
    }
}