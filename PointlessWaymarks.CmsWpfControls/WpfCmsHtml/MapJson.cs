using NetTopologySuite.Features;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsWpfControls.WpfCmsHtml;

public record MapJsonBoundsDto(SpatialBounds Bounds, string MessageType = "JsonBounds");

public record MapJsonCoordinateDto(double Latitude, double Longitude, string MessageType = "Coordinate");

public record MapJsonFeatureDto(Guid Identifier, string MessageType = "Feature");

public record MapJsonFeatureListDto(List<Guid> IdentifierList, string MessageType = "FeatureList");

public record MapJsonNewFeatureCollectionDto(
    Guid Identifier,
    SpatialBounds Bounds,
    List<FeatureCollection> GeoJsonLayers,
    string MessageType = "NewFeatureCollection");

public static class MapJson
{
    public static async Task<MapJsonNewFeatureCollectionDto> NewMapFeatureCollectionDto(
        string featureCollection)
    {
        var contentFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(featureCollection);

        var envelope = GeoJsonTools.GeometryBoundingBox(GeoJsonTools.GeoJsonToGeometries(featureCollection));

        return await NewMapFeatureCollectionDto(contentFeatureCollection.AsList(),
            SpatialBounds.FromEnvelope(envelope));
    }

    public static async Task<MapJsonNewFeatureCollectionDto> NewMapFeatureCollectionDto(
        List<FeatureCollection> featureCollections,
        SpatialBounds? bounds)
    {
        bounds ??= new SpatialBounds(await UserSettingsSingleton.CurrentSettings().DefaultLatitudeValidated(),
            await UserSettingsSingleton.CurrentSettings().DefaultLongitudeValidated(),
            await UserSettingsSingleton.CurrentSettings().DefaultLatitudeValidated(),
            await UserSettingsSingleton.CurrentSettings().DefaultLongitudeValidated());

        var expandedBounds = bounds.ExpandToMinimumMeters(1000);

        var mapJsonDto = new MapJsonNewFeatureCollectionDto(Guid.NewGuid(), expandedBounds, featureCollections);
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

    public static async Task<string> NewMapFeatureCollectionDtoSerialized(List<FeatureCollection> featureCollections,
        SpatialBounds? bounds)
    {
        var mapJsonDto =
            await GeoJsonTools.SerializeWithGeoJsonSerializer(await NewMapFeatureCollectionDto(featureCollections, bounds));

        await BracketCodeCommon.ProcessCodesForSite(mapJsonDto).ConfigureAwait(false);

        return mapJsonDto;
    }
}