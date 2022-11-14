using GeoTimeZone;

namespace PointlessWaymarks.SpatialTools;

public static class TimeTools
{
    public static DateTime LocalTimeFromUtcAndLocation(DateTime utcDateTime, double latitude, double longitude)
    {
        var photoLocationTimezoneIanaIdentifier =
            TimeZoneLookup.GetTimeZone(latitude, longitude);
        var photoLocationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(photoLocationTimezoneIanaIdentifier.Result);
        return TimeZoneInfo.ConvertTime(utcDateTime, photoLocationTimeZone);
    }
}