using System.Globalization;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.Spatial;

public static class SpatialHelpers
{


    public static bool IsApproximatelyEqualTo(this double initialValue, double value,
        double maximumDifferenceAllowed)
    {
        // Handle comparisons of floating point values that may not be exactly the same
        return Math.Abs(initialValue - value) < maximumDifferenceAllowed;
    }

    public static bool IsApproximatelyEqualTo(this double? initialValue, double? value,
        double maximumDifferenceAllowed)
    {
        if (initialValue == null && value == null) return true;
        if (initialValue != null && value == null) return false;
        if (initialValue == null /*&& value != null*/) return false;
        // ReSharper disable PossibleInvalidOperationException Checked above
        return initialValue.Value.IsApproximatelyEqualTo(value!.Value, maximumDifferenceAllowed);
        // ReSharper restore PossibleInvalidOperationException
    }

    /// <summary>
    ///     Uses reflection to look for Latitude, Longitude and Elevation properties on an object and rounds them to 6 digits.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="toProcess"></param>
    /// <returns></returns>
    public static T RoundSpatialValues<T>(T toProcess)
    {
        if (toProcess == null) return toProcess;

        var positionPropertyNames = new List<string> { "Latitude", "Longitude" };

        var positionProperties = typeof(T).GetProperties().Where(x =>
            (x.PropertyType == typeof(double) || x.PropertyType == typeof(double?)) && x.GetSetMethod() != null &&
            positionPropertyNames.Any(y => x.Name.EndsWith(y))).ToList();

        foreach (var loopProperty in positionProperties)
        {
            if (loopProperty.GetValue(toProcess) == null) continue;
            var current = (double)loopProperty.GetValue(toProcess)!;
            loopProperty.SetValue(toProcess, Math.Round(current, 6));
        }

        var distancePropertyNames = new List<string> { "Distance" };

        var distanceProperties = typeof(T).GetProperties().Where(x =>
            (x.PropertyType == typeof(double) || x.PropertyType == typeof(double?)) && x.GetSetMethod() != null &&
            distancePropertyNames.Any(y => x.Name.EndsWith(y))).ToList();

        foreach (var loopProperty in distanceProperties)
        {
            if (loopProperty.GetValue(toProcess) == null) continue;
            var current = (double)loopProperty.GetValue(toProcess)!;
            loopProperty.SetValue(toProcess, Math.Round(current, 2));
        }

        var elevationPropertyNames = new List<string> { "Elevation" };

        var elevationProperties = typeof(T).GetProperties().Where(x =>
            (x.PropertyType == typeof(double) || x.PropertyType == typeof(double?)) && x.GetSetMethod() != null &&
            elevationPropertyNames.Any(y => x.Name.EndsWith(y))).ToList();

        foreach (var loopProperty in elevationProperties)
        {
            if (loopProperty.GetValue(toProcess) == null) continue;
            var current = (double)loopProperty.GetValue(toProcess)!;
            loopProperty.SetValue(toProcess, Math.Round(current, 0));
        }

        return toProcess;
    }



    public static async Task<string> SerializeFeatureToGeoJson(IFeature feature)
    {
        var collection = new FeatureCollection { feature };

        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            GeoJsonTools.Wgs84GeometryFactory(), 3);

        await using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        serializer.Serialize(jsonWriter, collection);

        return stringWriter.ToString();
    }
}