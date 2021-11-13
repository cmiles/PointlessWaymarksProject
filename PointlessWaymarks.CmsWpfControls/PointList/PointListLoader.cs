using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.PointList;

public class PointListLoader : ContentListLoaderBase
{
    public PointListLoader(int? partialLoadQuantity) : base("Points", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = new List<DataNotificationContentType>
        {
            DataNotificationContentType.Point
        };
    }

    public override async Task<List<object>> LoadItems(IProgress<string> progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading Point Content from DB - Max {PartialLoadQuantity} Items");
            var returnItems = (await db.PointContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

            AllItemsLoaded = await db.PointContents.CountAsync() <= returnItems.Count;

            return returnItems;
        }

        progress?.Report("Loading All Point Content from DB");

        AllItemsLoaded = true;

        return (await db.PointContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }
}