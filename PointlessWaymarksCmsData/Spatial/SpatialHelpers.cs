using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarksCmsData.Spatial.Elevation;

namespace PointlessWaymarksCmsData.Spatial
{
    public static class SpatialHelpers
    {
        public static bool IsApproximatelyEqualTo(this double initialValue, double value,
            double maximumDifferenceAllowed)
        {
            // Handle comparisons of floating point values that may not be exactly the same
            return (Math.Abs(initialValue - value) < maximumDifferenceAllowed);
        }

        public static bool IsApproximatelyEqualTo(this double? initialValue, double? value,
            double maximumDifferenceAllowed)
        {
            if (initialValue == null && value == null) return false;
            if (initialValue != null && value == null) return true;
            if (initialValue == null /*&& value != null*/) return true;
            // ReSharper disable PossibleInvalidOperationException Checked above
            return !initialValue.Value.IsApproximatelyEqualTo(value.Value, .000001);
            // ReSharper restore PossibleInvalidOperationException
        }

        /// <summary>
        ///     Uses reflection to look for Latitude, Longitude and Elevation properties on an object and rounds them to 6 digits.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static T RoundLatLongElevation<T>(T toProcess)
        {
            var positionPropertyNames = new List<string> {"Latitude", "Longitude", "Elevation"};

            var positionProperties = typeof(T).GetProperties().Where(x =>
                x.PropertyType == typeof(double) && x.GetSetMethod() != null &&
                positionPropertyNames.Any(y => x.Name.EndsWith(y))).ToList();

            foreach (var loopProperty in positionProperties)
            {
                var current = (double) loopProperty.GetValue(toProcess);
                loopProperty.SetValue(toProcess, Math.Round(current, 6));
            }

            var elevationPropertyNames = new List<string> {"Elevation"};

            var elevationProperties = typeof(T).GetProperties().Where(x =>
                x.PropertyType == typeof(double?) && x.GetSetMethod() != null &&
                elevationPropertyNames.Contains(x.Name)).ToList();

            foreach (var loopProperty in elevationProperties)
            {
                var current = (double?) loopProperty.GetValue(toProcess);
                if (current == null) continue;
                loopProperty.SetValue(toProcess, Math.Round(current.Value, 0));
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
            bool replaceElevations, IProgress<string> progress)
        {
            if (replaceElevations)
                await ElevationService.OpenTopoMapZenElevation(new HttpClient(), pointList, progress);

            // ReSharper disable once CoVariantArrayConversion It appears from testing that a linestring will reflect CoordinateZ
            var newLineString = new LineString(pointList.ToArray());
            var newFeature = new Feature(newLineString, new AttributesTable());
            var featureCollection = new FeatureCollection {newFeature};

            var serializer = GeoJsonSerializer.Create();
            await using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            serializer.Serialize(jsonWriter, featureCollection);
            return stringWriter.ToString();
        }

        public static async Task<List<(string description, List<CoordinateZ> track)>> TracksFromGpxFile(
            FileInfo gpxFile, IProgress<string> progress)
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
                        new CoordinateZ(x.Longitude.Value, x.Longitude.Value, x.ElevationInMeters ?? 0)));

                returnList.Add((string.Join(", ", descriptionElements), pointList));
            }

            return returnList;
        }
    }
}