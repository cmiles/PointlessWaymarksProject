using NetTopologySuite.Geometries;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.SpatialTools;

public static class DistanceTools
{
    public static double GetDistanceInMeters(double longitude, double latitude,
        double otherLongitude, double otherLatitude)
    {
        var d1 = latitude * (Math.PI / 180.0);
        var num1 = longitude * (Math.PI / 180.0);
        var d2 = otherLatitude * (Math.PI / 180.0);
        var num2 = otherLongitude * (Math.PI / 180.0) - num1;
        var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                 Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

        return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
    }


    public static LineStatsInImperial LineStatsInImperialFromCoordinateList(List<CoordinateZ> line)
    {
        return LineStatsInImperialFromMetricStats(LineStatsInMetricFromCoordinateList(line));
    }

    public static LineStatsInImperial LineStatsInImperialFromMetricStats(LineStatsInMeters metricStats)
    {
        return new LineStatsInImperial(metricStats.Length.MetersToMiles(), metricStats.ElevationClimb.MetersToFeet(),
            metricStats.ElevationDescent.MetersToFeet(), metricStats.MaximumElevation.MetersToFeet(),
            metricStats.MinimumElevation.MetersToFeet());
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
            length += GetDistanceInMeters(previousPoint.X, previousPoint.Y,
                loopPoint.X, loopPoint.Y);
            if (previousPoint.Z < loopPoint.Z) climb += loopPoint.Z - previousPoint.Z;
            else descent += previousPoint.Z - loopPoint.Z;

            maxElevation = Math.Max(loopPoint.Z, maxElevation);
            minElevation = Math.Min(loopPoint.Z, minElevation);

            previousPoint = loopPoint;
        }

        return new LineStatsInMeters(length, climb, descent, maxElevation, minElevation);
    }

    public record LineStatsInImperial(double Length, double ElevationClimb, double ElevationDescent,
        double MaximumElevation, double MinimumElevation);

    public record LineStatsInMeters(double Length, double ElevationClimb, double ElevationDescent,
        double MaximumElevation, double MinimumElevation);
}