using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.VideoList;

public class VideoListLoader : ContentListLoaderBase
{
    public VideoListLoader(int? partialLoadQuantity) : base("Videos", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = new List<DataNotificationContentType> { DataNotificationContentType.Video };
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading Video Content from DB - Max {PartialLoadQuantity} Items");
            var returnItems = (await db.VideoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

            AllItemsLoaded = await db.VideoContents.CountAsync() <= returnItems.Count;

            return returnItems;
        }

        progress?.Report("Loading All Video Content from DB");

        AllItemsLoaded = true;

        return (await db.VideoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }
}