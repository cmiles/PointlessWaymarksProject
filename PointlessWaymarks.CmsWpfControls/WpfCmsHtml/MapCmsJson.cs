using System.IO;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.WpfCmsHtml;

public static class MapCmsJson
{
    public static Envelope GetBounds(List<IContentListItem> toMeasure)
    {
        var boundsKeeper = new List<Point>();

        foreach (var loopElements in toMeasure)
            switch (loopElements)
            {
                case GeoJsonListListItem { DbEntry.GeoJson: not null } mapGeoJson:
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMaxLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMinLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMinLatitude));
                    break;
                case LineListListItem { DbEntry.Line: not null } mapLine:
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMaxLongitude,
                        mapLine.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMinLongitude,
                        mapLine.DbEntry.InitialViewBoundsMinLatitude));
                    break;
            }

        if (toMeasure.Any(x => x is PointListListItem))
            foreach (var loopElements in toMeasure.Where(x => x is PointListListItem).Cast<PointListListItem>()
                         .ToList())
                boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude, loopElements.DbEntry.Latitude));

        if (toMeasure.Any(x => x is PhotoListListItem))
            foreach (var loopElements in toMeasure.Where(x => x is PhotoListListItem).Cast<PhotoListListItem>()
                         .ToList())
            {
                if (loopElements.DbEntry.Latitude is null || loopElements.DbEntry.Longitude is null) continue;

                boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude.Value, loopElements.DbEntry.Latitude.Value));
            }

        return SpatialConverters.PointBoundingBox(boundsKeeper);
    }

    public static async Task<MapJsonNewFeatureCollectionDto> NewMapFeatureCollectionDto(
        List<FeatureCollection> featureCollections,
        SpatialBounds? bounds, string messageType = "NewFeatureCollectionAndCenter")
    {
        bounds ??= new SpatialBounds(await UserSettingsSingleton.CurrentSettings().DefaultLatitudeValidated(),
            await UserSettingsSingleton.CurrentSettings().DefaultLongitudeValidated(),
            await UserSettingsSingleton.CurrentSettings().DefaultLatitudeValidated(),
            await UserSettingsSingleton.CurrentSettings().DefaultLongitudeValidated());

        var expandedBounds = bounds.ExpandToMinimumMeters(1000);

        var mapJsonDto =
            new MapJsonNewFeatureCollectionDto(Guid.NewGuid(), expandedBounds, featureCollections, messageType);
        return mapJsonDto;
    }

    public static async Task<string> NewMapFeatureCollectionDtoSerialized(List<FeatureCollection> featureCollections,
        SpatialBounds? bounds, string messageType = "NewFeatureCollectionAndCenter")
    {
        var mapJsonDto =
            await GeoJsonTools.SerializeWithGeoJsonSerializer(
                await NewMapFeatureCollectionDto(featureCollections, bounds, messageType));

        await BracketCodeCommon.ProcessCodesForSite(mapJsonDto).ConfigureAwait(false);

        return mapJsonDto;
    }

    public static async Task<string> NewMapFeatureCollectionDtoSerialized(
        string featureCollection)
    {
        var contentFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(featureCollection);

        var envelope = GeoJsonTools.GeometryBoundingBox(GeoJsonTools.GeoJsonToGeometries(featureCollection));

        return await NewMapFeatureCollectionDtoSerialized(contentFeatureCollection.AsList(),
            SpatialBounds.FromEnvelope(envelope));
    }

    public static async Task<(SpatialBounds bounds, List<FeatureCollection> featureList, List<string> fileCopyList)>
        ProcessContentToMapInformation(
            List<IContentListItem> frozenItems)
    {
        var dbEntries = new List<object>();

        foreach (var loopElements in frozenItems)
            switch (loopElements)
            {
                case GeoJsonListListItem { DbEntry.GeoJson: not null } geoJson:
                    dbEntries.Add(geoJson.DbEntry);
                    break;
                case LineListListItem { DbEntry.Line: not null } line:
                    dbEntries.Add(line.DbEntry);
                    break;
                case PointListListItem point:
                    dbEntries.Add(point.DbEntry);
                    break;
                case PhotoListListItem { DbEntry.Latitude: not null, DbEntry.Longitude: not null } photo:
                    dbEntries.Add(photo.DbEntry);
                    break;
            }

        return await ProcessContentToMapInformation(dbEntries);
    }

    /// <summary>
    ///     If your processing starts with IContentListItems use the overload that takes those - this version must query the
    ///     database for point information and process on disk content for photo image information.
    /// </summary>
    /// <param name="dbEntries"></param>
    /// <returns></returns>
    public static async Task<(SpatialBounds bounds, List<FeatureCollection> featureList, List<string> fileCopyList)>
        ProcessContentToMapInformation(
            List<object> dbEntries)
    {
        var geoJsonList = new List<FeatureCollection>();

        var boundsKeeper = new List<Point>();

        foreach (var loopElements in dbEntries)
            switch (loopElements)
            {
                case GeoJsonContent { GeoJson: not null } mapGeoJson:
                    var featureCollection =
                        GeoJsonTools.DeserializeStringToFeatureCollection(mapGeoJson.GeoJson);
                    foreach (var feature in featureCollection)
                        feature.Attributes.Add("displayId", mapGeoJson.ContentId);
                    geoJsonList.Add(featureCollection);
                    boundsKeeper.Add(new Point(mapGeoJson.InitialViewBoundsMaxLongitude,
                        mapGeoJson.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapGeoJson.InitialViewBoundsMinLongitude,
                        mapGeoJson.InitialViewBoundsMinLatitude));
                    break;
                case LineContent mapLine:
                    var lineFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(mapLine.Line);
                    foreach (var feature in lineFeatureCollection)
                        feature.Attributes.Add("displayId", mapLine.ContentId);
                    geoJsonList.Add(lineFeatureCollection);
                    geoJsonList.Add(lineFeatureCollection);
                    boundsKeeper.Add(new Point(mapLine.InitialViewBoundsMaxLongitude,
                        mapLine.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapLine.InitialViewBoundsMinLongitude,
                        mapLine.InitialViewBoundsMinLatitude));
                    break;
            }

        var db = await Db.Context();

        var points = await dbEntries.Where(x => x is PointContent).Cast<PointContent>()
            .SelectInSequenceAsync(async x => await Db.PointContentDtoFromPoint(x, db));

        var pointDtos = dbEntries.Where(x => x is PointContentDto).Cast<PointContentDto>().Union(points)
            .OrderBy(x => x.Title).ToList();

        if (pointDtos.Any())
        {
            var featureCollection = new FeatureCollection();

            foreach (var loopElements in pointDtos)
            {
                featureCollection.Add(new Feature(
                    PointTools.Wgs84Point(loopElements.Longitude, loopElements.Latitude,
                        loopElements.Elevation ?? 0),
                    new AttributesTable(new Dictionary<string, object>
                    {
                        { "title", loopElements.Title ?? string.Empty },
                        { "displayId", loopElements.ContentId }
                    })));
                boundsKeeper.Add(new Point(loopElements.Longitude, loopElements.Latitude));
            }

            geoJsonList.Add(featureCollection);
        }

        var filesToCopy = new List<string>();

        var photos = dbEntries.Where(x => x is PhotoContent).Cast<PhotoContent>()
            .Where(x => x.Latitude is not null && x.Longitude is not null).OrderBy(x => x.Title).ToList();

        if (photos.Any())
        {
            var featureCollection = new FeatureCollection();

            foreach (var loopElements in photos)
            {
                string description;

                var smallImageFile = loopElements.MainPicture != null
                    ? PictureAssetProcessing.ProcessPictureDirectory(loopElements.MainPicture.Value)?.SmallPicture
                        ?.File?.FullName
                    : null;

                if (smallImageFile != null)
                {
                    filesToCopy.Add(smallImageFile);

                    description = $"""
                                   <img src="https://[[VirtualDomain]]/{Path.GetFileName(smallImageFile)}"/>
                                   """;
                }
                else
                {
                    description = $"""
                                    <p>{loopElements.Summary}</p>
                                   """;
                }

                featureCollection.Add(new Feature(
                    PointTools.Wgs84Point(loopElements.Longitude.Value, loopElements.Latitude.Value,
                        loopElements.Elevation ?? 0),
                    new AttributesTable(new Dictionary<string, object>
                    {
                        { "title", loopElements.Title ?? string.Empty },
                        { "description", description },
                        { "displayId", loopElements.ContentId }
                    })));
                boundsKeeper.Add(new Point(loopElements.Longitude.Value, loopElements.Latitude.Value));
            }

            geoJsonList.Add(featureCollection);
        }

        var contentBounds = SpatialConverters.PointBoundingBox(boundsKeeper);

        return (SpatialBounds.FromEnvelope(contentBounds), geoJsonList, filesToCopy);
    }
}