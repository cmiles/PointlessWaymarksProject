using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite;
using NetTopologySuite.Geometries;

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
    }
}