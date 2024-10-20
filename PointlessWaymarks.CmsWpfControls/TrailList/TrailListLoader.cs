using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.TrailList;

public class TrailListLoader : ContentListLoaderBase
{
    public TrailListLoader(int? partialLoadQuantity) : base("Trails", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = [DataNotificationContentType.Trail];
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading Trail Content from DB - Max {PartialLoadQuantity} Items");
            var returnItems = (await db.TrailContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

            AllItemsLoaded = await db.TrailContents.CountAsync() <= returnItems.Count;

            return returnItems;
        }

        progress?.Report("Loading All Trail Content from DB");

        AllItemsLoaded = true;

        return (await db.TrailContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }
}