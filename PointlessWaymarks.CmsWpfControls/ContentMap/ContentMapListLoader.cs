using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsWpfControls.ContentMap;

public class ContentMapListLoader(string headerName, List<Guid> contentIdsToLoad)
    : ContentListLoaderBase(headerName, null)
{
    public List<Guid> ContentIdsToLoad { get; set; } = contentIdsToLoad;

    public override async Task<List<object>> LoadItems(IProgress<string>? progress = null)
    {
        var db = await Db.Context();

        return await db.ContentFromContentIds(ContentIdsToLoad, false);
    }
}