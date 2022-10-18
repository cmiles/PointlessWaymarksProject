using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace PointlessWaymarks.SpatialTools;

public static class LineTools
{
    public static List<CoordinateZ> CoordinateListFromGeoJsonFeatureCollectionWithLinestring(string geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson)) return new List<CoordinateZ>();

        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            GeoJsonTools.Wgs84GeometryFactory(), 3);

        using var stringReader = new StringReader(geoJson);
        using var jsonReader = new JsonTextReader(stringReader);
        var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);
        if (featureCollection == null || featureCollection.Count < 1) return new List<CoordinateZ>();

        var possibleLine = featureCollection.FirstOrDefault(x => x.Geometry is LineString);

        if (possibleLine == null) return new List<CoordinateZ>();

        var geoLine = (LineString)possibleLine.Geometry;

        return geoLine.Coordinates.Select(x => new CoordinateZ(x.X, x.Y, x.Z)).ToList();
    }


    public static async Task<string> GeoJsonWithLineStringFromCoordinateList(List<CoordinateZ> pointList,
        bool replaceElevations, IProgress<string>? progress = null)
    {
        if (replaceElevations)
            await ElevationService.OpenTopoMapZenElevation(pointList, progress)
                .ConfigureAwait(false);

        // ReSharper disable once CoVariantArrayConversion It appears from testing that a linestring will reflect CoordinateZ
        var newLineString = new LineString(pointList.ToArray());
        var newFeature = new Feature(newLineString, new AttributesTable());
        var featureCollection = new FeatureCollection { newFeature };

        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            GeoJsonTools.Wgs84GeometryFactory(), 3);
        await using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        serializer.Serialize(jsonWriter, featureCollection);
        return stringWriter.ToString();
    }
}