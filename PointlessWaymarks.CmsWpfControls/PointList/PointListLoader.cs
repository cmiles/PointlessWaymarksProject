using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.PointList;

public class PointListLoader : ContentListLoaderBase
{
    public PointListLoader(int? partialLoadQuantity) : base("Points", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = [DataNotificationContentType.Point];
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading Point Content from DB - Max {PartialLoadQuantity} Items");
            var dbPoints = await db.PointContents.Take(PartialLoadQuantity.Value).ToListAsync();

            var returnPointDtos = new List<PointContentDto>();

            foreach (var loopPoint in dbPoints) returnPointDtos.Add(await Db.PointContentDtoFromPoint(loopPoint, db));

            AllItemsLoaded = await db.PointContents.CountAsync() <= dbPoints.Count;

            return returnPointDtos.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn).Cast<object>().ToList();
        }

        progress?.Report("Loading All Point Content from DB");

        AllItemsLoaded = true;

        return (await db.PointContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }
}