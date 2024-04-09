using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.PointDetailDataModels;

namespace PointlessWaymarks.CmsData.ContentHtml.PointHtml;

public static class PointData
{
    public static async Task<string> JsonDataToString()
    {
        var db = await Db.Context().ConfigureAwait(false);
        var allPointIds = await db.PointContents.Where(x => !x.IsDraft).Select(x => x.ContentId).ToListAsync()
            .ConfigureAwait(false);
        var extendedPointInformation = await Db.PointContentDto(allPointIds, db).ConfigureAwait(false);
        var settings = UserSettingsSingleton.CurrentSettings();

        return JsonSerializer.Serialize(extendedPointInformation.Select(x => new
        {
            x.ContentId,
            x.Title,
            x.Summary,
            x.Longitude,
            x.Latitude,
            x.Slug,
            PointPageUrl = settings.PointPageUrl(x),
            SmallPictureUrl = x.MainPicture == null
                ? string.Empty
                : new PictureSiteInformation(x.MainPicture.Value).Pictures?.SmallPicture?.SiteUrl ?? string.Empty,
            x.MapLabel,
            DetailTypeString = string.Join(", ", PointDetailUtilities.PointDtoTypeList(x)),
            x.MapIconName,
            x.MapMarkerColor
        }).ToList());
    }
}