using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData.ContentHtml.MapComponentData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CmsData.Spatial;

namespace PointlessWaymarks.CmsData.Content;

public static class MapComponentGenerator
{
    public static async Task<(GenerationReturn generationReturn, MapComponentDto? mapDto)> GenerateAllLinesData(
        Progress<string>? progress = null)
    {
        var frozenNow = DateTime.Now;
        var allLines = new MapComponent
        {
            Summary = "All Lines",
            Title = "All Lines",
            ContentVersion = Db.ContentVersionDateTime(),
            CreatedBy = "Map Generator",
            CreatedOn = frozenNow,
            ContentId = new Guid("00000000-0000-0000-0000-000000000001")
        };

        var boundsKeeper = new List<Point>();
        var elementList = new List<MapElement>();

        var db = await Db.Context();

        var dbLines = await db.LineContents.Where(x => !x.IsDraft).OrderByDescending(x => x.CreatedOn).AsNoTracking().ToListAsync();

        foreach (var mapLine in dbLines)
        {
            boundsKeeper.Add(new Point(mapLine.InitialViewBoundsMaxLongitude,
                mapLine.InitialViewBoundsMaxLatitude));
            boundsKeeper.Add(new Point(mapLine.InitialViewBoundsMinLongitude,
                mapLine.InitialViewBoundsMinLatitude));

            elementList.Add(new MapElement
            {
                ElementContentId = mapLine.ContentId,
                IncludeInDefaultView = true,
                IsFeaturedElement = false,
                MapComponentContentId = allLines.ContentId
            });
        }

        var bounds = SpatialConverters.PointBoundingBox(boundsKeeper);

        allLines.InitialViewBoundsMaxLatitude = bounds.MaxY;
        allLines.InitialViewBoundsMaxLongitude = bounds.MaxX;
        allLines.InitialViewBoundsMinLatitude = bounds.MinY;
        allLines.InitialViewBoundsMinLongitude = bounds.MinX;
        allLines.UpdateNotesFormat = ContentFormatDefaults.Content.ToString();

        var mapDto = new MapComponentDto(allLines, elementList);

        var validationReturn = await Validate(mapDto).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(mapDto);

        var savedComponent = await Db.SaveMapComponent(mapDto).ConfigureAwait(false);

        await Export.WriteMapComponentContentData(savedComponent, progress).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Map Component Generator", DataNotificationContentType.Map,
            DataNotificationUpdateType.LocalContent, new List<Guid> { mapDto.ContentId });

        return (
            GenerationReturn.Success(
                $"Saved and Generated Map Component {mapDto.ContentId} - {allLines.Title}"),
            mapDto);
    }

    public static async Task<(GenerationReturn generationReturn, MapComponentDto? mapDto)> SaveAndGenerateData(
        MapComponentDto toSave, DateTime? generationVersion, IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);

        var savedComponent = await Db.SaveMapComponent(toSave).ConfigureAwait(false);

        await Export.WriteMapComponentContentData(savedComponent, progress).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Map Component Generator", DataNotificationContentType.Map,
            DataNotificationUpdateType.LocalContent, new List<Guid> { savedComponent.ContentId });

        return (
            GenerationReturn.Success(
                $"Saved and Generated Map Component {savedComponent.ContentId} - {savedComponent.Title}"),
            savedComponent);
    }

    public static async Task<GenerationReturn> Validate(MapComponentDto mapComponent)
    {
        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                mapComponent.ContentId);

        var commonContentCheck = await CommonContentValidation.ValidateMapComponent(mapComponent).ConfigureAwait(false);
        if (!commonContentCheck.Valid)
            return GenerationReturn.Error(commonContentCheck.Explanation, mapComponent.ContentId);

        var updateFormatCheck =
            CommonContentValidation.ValidateUpdateContentFormat(mapComponent.UpdateNotesFormat);
        if (!updateFormatCheck.Valid)
            return GenerationReturn.Error(updateFormatCheck.Explanation, mapComponent.ContentId);

        return GenerationReturn.Success("GeoJson Content Validation Successful");
    }
}