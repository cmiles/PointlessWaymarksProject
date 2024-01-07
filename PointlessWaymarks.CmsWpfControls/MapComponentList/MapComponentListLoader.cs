using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList;

public class MapComponentListLoader : ContentListLoaderBase
{
    public MapComponentListLoader(int? partialLoadQuantity) : base("Maps", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = [DataNotificationContentType.Map];
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading Map Content from DB - Max {PartialLoadQuantity} Items");
            var returnItems = (await db.MapComponents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

            AllItemsLoaded = await db.MapComponents.CountAsync() <= returnItems.Count;

            return returnItems;
        }

        progress?.Report("Loading All Map Content from DB");

        AllItemsLoaded = true;

        return (await db.MapComponents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn).ToListAsync())
            .Cast<object>().ToList();
    }
}