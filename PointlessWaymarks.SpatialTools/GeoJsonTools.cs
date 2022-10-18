using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace PointlessWaymarks.SpatialTools;

public static class GeoJsonTools
{
    public static FeatureCollection DeserializeFileToFeatureCollection(string fileName)
    {
        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            Wgs84GeometryFactory(), 3);

        using var intersectFileStream = File.Open(fileName, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite);
        using var intersectStreamReader = new StreamReader(intersectFileStream);
        using var intersectJsonReader = new JsonTextReader(intersectStreamReader);
        return serializer.Deserialize<FeatureCollection>(intersectJsonReader);
    }

    public static FeatureCollection DeserializeToFeatureCollection(string geoJsonString)
    {
        if (string.IsNullOrEmpty(geoJsonString)) return new FeatureCollection();

        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            Wgs84GeometryFactory(), 3);

        using var stringReader = new StringReader(geoJsonString);
        using var jsonReader = new JsonTextReader(stringReader);
        return serializer.Deserialize<FeatureCollection>(jsonReader);
    }


    public static GeometryFactory Wgs84GeometryFactory()
    {
        return NtsGeometryServices.Instance.CreateGeometryFactory(new PrecisionModel(), 4326);
    }
}