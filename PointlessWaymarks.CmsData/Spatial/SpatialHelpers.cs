using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.CmsData.Spatial.Elevation;

namespace PointlessWaymarks.CmsData.Spatial
{
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
            if (initialValue == null && value == null) return false;
            if (initialValue != null && value == null) return true;
            if (initialValue == null /*&& value != null*/) return true;
            // ReSharper disable PossibleInvalidOperationException Checked above
            return !initialValue.Value.IsApproximatelyEqualTo(value!.Value, maximumDifferenceAllowed);
            // ReSharper restore PossibleInvalidOperationException
        }

        public static async Task<string> SerializeFeatureCollection(FeatureCollection featureCollection)
        {
            var serializer = GeoJsonSerializer.Create(Wgs84GeometryFactory(), 3);

            await using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            serializer.Serialize(jsonWriter, featureCollection);

            return stringWriter.ToString();
        }

        public static async Task<string> SerializeAsGeoJson(object toSerialize)
        {
            var serializer = GeoJsonSerializer.Create(Wgs84GeometryFactory(), 3);

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

            var positionPropertyNames = new List<string> {"Latitude", "Longitude"};

            var positionProperties = typeof(T).GetProperties().Where(x =>
                (x.PropertyType == typeof(double) || x.PropertyType == typeof(double?)) && x.GetSetMethod() != null &&
                positionPropertyNames.Any(y => x.Name.EndsWith(y))).ToList();

            foreach (var loopProperty in positionProperties)
            {
                if (loopProperty.GetValue(toProcess) == null) continue;
                var current = (double) loopProperty.GetValue(toProcess)!;
                loopProperty.SetValue(toProcess, Math.Round(current, 6));
            }

            var distancePropertyNames = new List<string> {"Distance"};

            var distanceProperties = typeof(T).GetProperties().Where(x =>
                (x.PropertyType == typeof(double) || x.PropertyType == typeof(double?)) && x.GetSetMethod() != null &&
                distancePropertyNames.Any(y => x.Name.EndsWith(y))).ToList();

            foreach (var loopProperty in distanceProperties)
            {
                if (loopProperty.GetValue(toProcess) == null) continue;
                var current = (double) loopProperty.GetValue(toProcess)!;
                loopProperty.SetValue(toProcess, Math.Round(current, 2));
            }

            var elevationPropertyNames = new List<string> {"Elevation"};

            var elevationProperties = typeof(T).GetProperties().Where(x =>
                (x.PropertyType == typeof(double) || x.PropertyType == typeof(double?)) && x.GetSetMethod() != null &&
                elevationPropertyNames.Any(y => x.Name.EndsWith(y))).ToList();

            foreach (var loopProperty in elevationProperties)
            {
                if (loopProperty.GetValue(toProcess) == null) continue;
                var current = (double) loopProperty.GetValue(toProcess)!;
                loopProperty.SetValue(toProcess, Math.Round(current, 0));
            }

            return toProcess;
        }

        public static GeometryFactory Wgs84GeometryFactory()
        {
            return NtsGeometryServices.Instance.CreateGeometryFactory(new PrecisionModel(), 4326);
        }

        public static Point Wgs84Point(double x, double y, double z)
        {
            return Wgs84GeometryFactory().CreatePoint(new CoordinateZ(x, y, z));
        }

        public static async Task<string> GeoJsonWithLineStringFromCoordinateList(List<CoordinateZ> pointList,
            bool replaceElevations, IProgress<string>? progress = null)
        {
            if (replaceElevations)
                await ElevationService.OpenTopoMapZenElevation(new HttpClient(), pointList, progress);

            // ReSharper disable once CoVariantArrayConversion It appears from testing that a linestring will reflect CoordinateZ
            var newLineString = new LineString(pointList.ToArray());
            var newFeature = new Feature(newLineString, new AttributesTable());
            var featureCollection = new FeatureCollection {newFeature};

            var serializer = GeoJsonSerializer.Create(Wgs84GeometryFactory(), 3);
            await using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            serializer.Serialize(jsonWriter, featureCollection);
            return stringWriter.ToString();
        }

        public static List<CoordinateZ> CoordinateListFromGeoJsonFeatureCollectionWithLinestring(string geoJson)
        {
            if (string.IsNullOrWhiteSpace(geoJson)) return new List<CoordinateZ>();

            var serializer = GeoJsonSerializer.Create(Wgs84GeometryFactory(), 3);

            using var stringReader = new StringReader(geoJson);
            using var jsonReader = new JsonTextReader(stringReader);
            var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);
            if (featureCollection == null || featureCollection.Count < 1) return new List<CoordinateZ>();

            var possibleLine = featureCollection.FirstOrDefault(x => x.Geometry is LineString);

            if (possibleLine == null) return new List<CoordinateZ>();

            var geoLine = (LineString) possibleLine.Geometry;

            return geoLine.Coordinates.Select(x => new CoordinateZ(x.X, x.Y, x.Z)).ToList();
        }

        public static async Task<string> ReplaceElevationsInGeoJsonWithLineString(string geoJson,
            [CanBeNull] IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(geoJson)) return string.Empty;

            var coordinateList = CoordinateListFromGeoJsonFeatureCollectionWithLinestring(geoJson);

            return await GeoJsonWithLineStringFromCoordinateList(coordinateList, true, progress);
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

            if (line == null || line.Count < 2)
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
            return new(metricStats.Length.MetersToMiles(), metricStats.ElevationClimb.MetersToFeet(),
                metricStats.ElevationDescent.MetersToFeet(), metricStats.MaximumElevation.MetersToFeet(),
                metricStats.MinimumElevation.MetersToFeet());
        }

        public record LineStatsInMeters(double Length, double ElevationClimb, double ElevationDescent,
            double MaximumElevation, double MinimumElevation);

        public record LineStatsInImperial(double Length, double ElevationClimb, double ElevationDescent,
            double MaximumElevation, double MinimumElevation);

        public static async Task<List<(string description, List<CoordinateZ> track)>> TracksFromGpxFile(
            FileInfo gpxFile, IProgress<string>? progress = null)
        {
            var returnList = new List<(string description, List<CoordinateZ>)>();

            if (gpxFile == null || !gpxFile.Exists) return returnList;

            GpxFile parsedGpx;

            try
            {
                parsedGpx = GpxFile.Parse(await File.ReadAllTextAsync(gpxFile.FullName),
                    new GpxReaderSettings
                    {
                        IgnoreUnexpectedChildrenOfTopLevelElement = true, IgnoreVersionAttribute = true
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var trackCounter = 1;

            foreach (var loopTracks in parsedGpx.Tracks)
            {
                var descriptionElements =
                    new List<string> {$"{trackCounter++}", loopTracks.Comment, loopTracks.Description, loopTracks.Name}
                        .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                var extensions = loopTracks.Extensions;

                if (extensions is ImmutableXElementContainer extensionsContainer)
                {
                    var timeString = extensionsContainer.FirstOrDefault(x => x.Name.LocalName.ToLower() == "time")
                        ?.Value;

                    if (!string.IsNullOrWhiteSpace(timeString) && DateTime.TryParse(timeString, out var resultDateTime))
                        descriptionElements.Add(DateTime.SpecifyKind(resultDateTime, DateTimeKind.Utc).ToLocalTime()
                            .ToString(CultureInfo.InvariantCulture));

                    var possibleLabelString = extensionsContainer
                        .FirstOrDefault(x => x.Name.LocalName.ToLower() == "label")?.Value;

                    if (!string.IsNullOrWhiteSpace(possibleLabelString)) descriptionElements.Add(possibleLabelString);
                }

                var pointList = new List<CoordinateZ>();

                foreach (var loopSegments in loopTracks.Segments)
                    pointList.AddRange(loopSegments.Waypoints.Select(x =>
                        new CoordinateZ(x.Longitude.Value, x.Latitude.Value, x.ElevationInMeters ?? 0)));

                returnList.Add((string.Join(", ", descriptionElements), pointList));
            }

            return returnList;
        }
    }
}