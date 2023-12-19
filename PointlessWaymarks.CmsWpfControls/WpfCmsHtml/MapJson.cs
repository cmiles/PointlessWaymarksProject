using NetTopologySuite.Features;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;

namespace PointlessWaymarks.CmsWpfControls.WpfCmsHtml;

public record MapJsonBoundsDto(GeoJsonData.SpatialBounds Bounds, string MessageType = "JsonBounds");

public record MapJsonCoordinateDto(double Latitude, double Longitude, string MessageType = "Coordinate");

public record MapJsonFeatureDto(Guid Identifier, string MessageType = "Feature");

public record MapJsonFeatureListDto(List<Guid> IdentifierList, string MessageType = "FeatureList");

public record MapJsonNewFeatureCollectionDto(
    Guid Identifier,
    GeoJsonData.SpatialBounds Bounds,
    List<FeatureCollection> GeoJsonLayers,
    string MessageType = "NewFeatureCollection");