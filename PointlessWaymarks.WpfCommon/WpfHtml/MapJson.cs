using NetTopologySuite.Features;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

public record MapJsonBoundsDto(SpatialBounds Bounds, string MessageType = "JsonBounds");

public record MapJsonCoordinateDto(double Latitude, double Longitude, string MessageType = "Coordinate");

public record MapJsonFeatureDto(Guid Identifier, string MessageType = "Feature");

public record MapJsonFeatureListDto(List<Guid> IdentifierList, string MessageType = "FeatureList");

public record MapJsonLoadElevationChartDataDto(
    List<LineElevationChartDataPoint> ElevationData,
    string MessageType = "LoadElevationChartData");

public record MapJsonNewFeatureCollectionDto(
    Guid Identifier,
    SpatialBounds Bounds,
    List<FeatureCollection> GeoJsonLayers,
    string MessageType = "NewFeatureCollectionAndCenter");

public static class MapJson
{
    public static MapJsonNewFeatureCollectionDto NewMapFeatureCollectionDto(
        List<FeatureCollection> featureCollections,
        SpatialBounds? bounds, string messageType = "NewFeatureCollectionAndCenter")
    {
        bounds ??= new SpatialBounds(32.12063, -110.52313, 32.12063, -110.52313);

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
                NewMapFeatureCollectionDto(featureCollections, bounds, messageType));

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
}