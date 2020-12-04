using System;

namespace PointlessWaymarksCmsData.Spatial
{
    public static class DistanceHelpers
    {
        public static double FeetToMeters(this double feet)
        {
            return feet * 0.3048;
        }

        public static double FeetToMeters(this double? feet)
        {
            if (feet == null) return 0;
            return feet.Value.FeetToMeters();
        }

        public static double MetersToFeet(this double meters)
        {
            return meters / 0.3048;
        }

        public static double MetersToFeet(this double? meters)
        {
            if (meters == null) return 0;
            return meters.Value.MetersToFeet();
        }

        public static double MetersToMiles(this double meters)
        {
            return meters / 1609.344;
        }

        public static double MetersToMiles(this double? meters)
        {
            if (meters == null) return 0;
            return meters.Value.MetersToFeet();
        }

        public static double MilesToMeters(this double miles)
        {
            return miles * 1609.344;
        }

        public static double MilesToMeters(this double? miles)
        {
            if (miles == null) return 0;
            return miles.Value.FeetToMeters();
        }

        public static double GetDistanceInMeters(double longitude, double latitude, double elevation, double otherLongitude, double otherLatitude, double otherElevation)
        {
            var d1 = latitude * (Math.PI / 180.0);
            var num1 = longitude * (Math.PI / 180.0);
            var d2 = otherLatitude * (Math.PI / 180.0);
            var num2 = otherLongitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3))) + Math.Abs(elevation - otherElevation);
        }
    }
}