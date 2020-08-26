﻿using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;

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

        /// <summary>
        ///     Uses reflection to look for Latitude, Longitude and Elevation properties on an object and rounds them to 6 digits.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static T RoundLatLongElevationToSixPlaces<T>(T toProcess)
        {
            var positionPropertyNames = new List<string> {"Latitude", "Longitude", "Elevation"};

            var positionProperties = typeof(T).GetProperties().Where(x =>
                    x.PropertyType == typeof(double) && x.GetSetMethod() != null &&
                    positionPropertyNames.Contains(x.Name))
                .ToList();

            foreach (var loopProperty in positionProperties)
            {
                var current = (double) loopProperty.GetValue(toProcess);
                loopProperty.SetValue(toProcess, Math.Round(current, 6));
            }

            return toProcess;
        }

        public static GeometryFactory Wgs84GeometryFactory()
        {
            return NtsGeometryServices.Instance.CreateGeometryFactory(new PrecisionModel(), 4326,
                DotSpatialAffineCoordinateSequenceFactory.Instance);
        }

        public static Point Wgs84Point(double x, double y, double z)
        {
            return Wgs84GeometryFactory().CreatePoint(new CoordinateZ(x, y, z));
        }
    }
}