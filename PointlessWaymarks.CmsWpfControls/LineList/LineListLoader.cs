using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.LineList;

public class LineListLoader : ContentListLoaderBase
{
    public LineListLoader(int? partialLoadQuantity) : base("Lines", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = new List<DataNotificationContentType> {DataNotificationContentType.Line};
    }

    public override async Task<List<object>> LoadItems(IProgress<string> progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading Line Content from DB - Max {PartialLoadQuantity} Items");
            var returnItems = (await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

            AllItemsLoaded = await db.LineContents.CountAsync() <= returnItems.Count;

            return returnItems;
        }

        progress?.Report("Loading All Line Content from DB");

        AllItemsLoaded = true;

        return (await db.LineContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }
}