using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonList;

public class GeoJsonListLoader : ContentListLoaderBase
{
    public GeoJsonListLoader(int? partialLoadQuantity) : base("GeoJson", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = [DataNotificationContentType.GeoJson];
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading GeoJson Content from DB - Max {PartialLoadQuantity} Items");
            var returnItems = (await db.GeoJsonContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

            AllItemsLoaded = await db.GeoJsonContents.CountAsync() <= returnItems.Count;

            return returnItems;
        }

        progress?.Report("Loading All GeoJson Content from DB");

        AllItemsLoaded = true;

        return (await db.GeoJsonContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }
}