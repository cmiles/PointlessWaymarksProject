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

    public static async Task<string> SerializeFeatureCollectionToGeoJson(FeatureCollection featureCollection)
    {
        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            GeoJsonTools.Wgs84GeometryFactory(), 3);

        await using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        serializer.Serialize(jsonWriter, featureCollection);

        return stringWriter.ToString();
    }

    public static async Task<string> SerializeWithGeoJsonSerializer(object toSerialize)
    {
        var serializer = GeoJsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented },
            GeoJsonTools.Wgs84GeometryFactory(), 3);

        await using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        serializer.Serialize(jsonWriter, toSerialize);

        return stringWriter.ToString();
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


    public static async Task<string> ReplaceElevationsInGeoJsonWithLineString(string geoJson,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(geoJson)) return string.Empty;

        var coordinateList = LineTools.CoordinateListFromGeoJsonFeatureCollectionWithLinestring(geoJson);

        return await LineTools.GeoJsonWithLineStringFromCoordinateList(coordinateList, true, progress).ConfigureAwait(false);
    }

    public static LineStatsInImperial LineStatsInImperialFromCoordinateList(List<CoordinateZ> line)
    {
        return LineStatsInImperialFromMetricStats(LineStatsInMetricFromCoordinateList(line));
    }

    public static LineStatsInMeters LineStatsInMetricFromCoordinateList(List<CoordinateZ> line)
    {
        double climb = 0;
        double descent = 0;
        double length = 0;
        double maxElevation = 0;
        double minElevation = 0;

        if (line.Count < 2)
            return new LineStatsInMeters(length, climb, descent, maxElevation, minElevation);

        var previousPoint = line[0];
        maxElevation = previousPoint.Z;
        minElevation = previousPoint.Z;

        foreach (var loopPoint in line.Skip(1))
        {
            length += DistanceHelpers.GetDistanceInMeters(previousPoint.X, previousPoint.Y, previousPoint.Z,
                loopPoint.X, loopPoint.Y, loopPoint.Z);
            if (previousPoint.Z < loopPoint.Z) climb += loopPoint.Z - previousPoint.Z;
            else descent += previousPoint.Z - loopPoint.Z;

            maxElevation = Math.Max(loopPoint.Z, maxElevation);
            minElevation = Math.Min(loopPoint.Z, minElevation);

            previousPoint = loopPoint;
        }

        return new LineStatsInMeters(length, climb, descent, maxElevation, minElevation);
    }

    public static LineStatsInImperial LineStatsInImperialFromMetricStats(LineStatsInMeters metricStats)
    {
        return new LineStatsInImperial(metricStats.Length.MetersToMiles(),
            metricStats.ElevationClimb.MetersToFeet(),
            metricStats.ElevationDescent.MetersToFeet(), metricStats.MaximumElevation.MetersToFeet(),
            metricStats.MinimumElevation.MetersToFeet());
    }

    public record LineStatsInMeters(double Length, double ElevationClimb, double ElevationDescent,
        double MaximumElevation, double MinimumElevation);

    public record LineStatsInImperial(double Length, double ElevationClimb, double ElevationDescent,
        double MaximumElevation, double MinimumElevation);



}