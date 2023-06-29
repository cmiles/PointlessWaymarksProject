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

    public static FeatureCollection DeserializeStringToFeatureCollection(string geoJsonString)
    {
        if (string.IsNullOrEmpty(geoJsonString)) return new FeatureCollection();

        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            Wgs84GeometryFactory(), 3);

        using var stringReader = new StringReader(geoJsonString);
        using var jsonReader = new JsonTextReader(stringReader);
        return serializer.Deserialize<FeatureCollection>(jsonReader);
    }

    public static List<Geometry> GeoJsonToGeometries(string geoJson)
    {
        var featureCollection = DeserializeStringToFeatureCollection(geoJson);

        return featureCollection.Select(x => Wgs84GeometryFactory().CreateGeometry(x.Geometry))
            .ToList();
    }

    public static Envelope GeometryBoundingBox(List<Geometry> geometries, Envelope? boundingBox = null)
    {
        boundingBox ??= new Envelope();
        foreach (var feature in geometries) boundingBox.ExpandToInclude(feature.EnvelopeInternal);

        return boundingBox;
    }

    public static Envelope GeometryBoundingBox(string lineString, Envelope? envelope = null)
    {
        var geometryList = LineStringToGeometries(lineString);
        return GeometryBoundingBox(geometryList, envelope);
    }

    public static List<Geometry> LineStringToGeometries(string lineString)
    {
        if (string.IsNullOrWhiteSpace(lineString)) return new List<Geometry>();

        var featureCollection = DeserializeStringToFeatureCollection(lineString);

        return featureCollection.Select(x => Wgs84GeometryFactory().CreateGeometry(x.Geometry))
            .ToList();
    }


    public static async Task<string> ReplaceElevationsInGeoJsonWithLineString(string geoJson,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(geoJson)) return string.Empty;

        var coordinateList = LineTools.CoordinateListFromGeoJsonFeatureCollectionWithLinestring(geoJson);

        return await LineTools.GeoJsonWithLineStringFromCoordinateList(coordinateList, true, progress)
            .ConfigureAwait(false);
    }

    public static async Task<string> SerializeListOfFeaturesCollectionToGeoJson(List<Feature> features)
    {
        var collectionBoundingBox = new Envelope();
        var collection = new FeatureCollection();

        foreach (var loopFeature in features)
        {
            collectionBoundingBox.ExpandToInclude(loopFeature.Geometry.Coordinate);
            collection.Add(loopFeature);
        }

        collection.BoundingBox = collectionBoundingBox;

        return await SerializeFeatureCollectionToGeoJson(collection);
    }

    public static async Task<string> SerializeListOfFeaturesCollectionToGeoJson(List<IFeature> features)
    {
        var collectionBoundingBox = new Envelope();
        var collection = new FeatureCollection();

        foreach (var loopFeature in features)
        {
            collectionBoundingBox.ExpandToInclude(loopFeature.Geometry.Coordinate);
            collection.Add(loopFeature);
        }

        collection.BoundingBox = collectionBoundingBox;

        return await SerializeFeatureCollectionToGeoJson(collection);
    }

    public static async Task<string> SerializeFeatureCollectionToGeoJson(FeatureCollection featureCollection)
    {
        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            Wgs84GeometryFactory(), 3);

        await using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        serializer.Serialize(jsonWriter, featureCollection);

        return stringWriter.ToString();
    }

    public static async Task<string> SerializeFeatureToGeoJson(IFeature feature)
    {
        var collection = new FeatureCollection { feature };

        return await SerializeFeatureCollectionToGeoJson(collection);
    }

    public static async Task<string> SerializeWithGeoJsonSerializer(object toSerialize)
    {
        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            Wgs84GeometryFactory(), 3);

        await using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        serializer.Serialize(jsonWriter, toSerialize);

        return stringWriter.ToString();
    }


    public static GeometryFactory Wgs84GeometryFactory()
    {
        return NtsGeometryServices.Instance.CreateGeometryFactory(new PrecisionModel(), 4326);
    }
}