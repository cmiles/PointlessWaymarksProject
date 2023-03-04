using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.FileList;

public class FileListLoader : ContentListLoaderBase
{
    public FileListLoader(int? partialLoadQuantity) : base("Files", partialLoadQuantity)
    {
        DataNotificationTypesToRespondTo = new List<DataNotificationContentType> {DataNotificationContentType.File};
    }

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var db = await Db.Context();

        if (PartialLoadQuantity != null)
        {
            progress?.Report($"Loading File Content from DB - Max {PartialLoadQuantity} Items");
            var returnItems = (await db.FileContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                .Take(PartialLoadQuantity.Value).ToListAsync()).Cast<object>().ToList();

            AllItemsLoaded = await db.FileContents.CountAsync() <= returnItems.Count;

            return returnItems;
        }

        progress?.Report("Loading All File Content from DB");

        AllItemsLoaded = true;

        return (await db.FileContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }
}