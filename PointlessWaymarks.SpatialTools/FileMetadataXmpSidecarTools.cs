using XmpCore;

namespace PointlessWaymarks.SpatialTools;

public static class FileMetadataXmpSidecarTools
{
    public static double? AltitudeFromXmpSidecar(IXmpMeta? sidecarXmpMeta)
    {
        if (sidecarXmpMeta == null) return null;

        double? altitude = null;

        var gpsAltitudeProperty = sidecarXmpMeta.GetProperty("http://ns.adobe.com/exif/1.0/", "exif:GPSAltitude");

        if (string.IsNullOrWhiteSpace(gpsAltitudeProperty?.Value)) return null;

        var gpsAltitude = gpsAltitudeProperty.Value;

        var gpsAltitudeRef = sidecarXmpMeta.GetProperty("http://ns.adobe.com/exif/1.0/", "exif:GPSAltitudeRef");

        var altitudeIsAboveSeaLevel = string.IsNullOrWhiteSpace(gpsAltitudeRef?.Value) || gpsAltitudeRef.Equals("0");

        var numerator = 0;
        var denominator = 0;

        if (gpsAltitude.Contains("/"))
        {
            var altitudeParts = gpsAltitude.Split("/");
            if (altitudeParts.Length == 2)
            {
                var numeratorParsed = int.TryParse(altitudeParts[0], out var parsedNumerator);
                var denominatorParsed = int.TryParse(altitudeParts[1], out var parsedDenominator);

                if (numeratorParsed && denominatorParsed)
                {
                    numerator = parsedNumerator;
                    denominator = parsedDenominator;
                }
            }
        }
        else
        {
            var parsed = int.TryParse(gpsAltitude, out var parsedAltitude);
            if (parsed)
            {
                numerator = parsedAltitude;
                denominator = 1;
            }
        }

        if (denominator != 0)
            altitude = altitudeIsAboveSeaLevel
                ? (double)numerator / denominator
                : (double)numerator / denominator * -1;

        return altitude;
    }

    public static async Task<(DateTime? createdOnLocal, DateTime? createdOnUtc)> CreatedOnLocalAndUtc(
        IXmpMeta sidecarXmpMeta)
    {
        var gps = CreatedOnLocalAndUtcFromXmpSidecarGpsTimeStamp(sidecarXmpMeta);

        if (gps.createdOnUtc != null && gps.createdOnLocal != null) return gps;

        var photoLocation = await LocationFromXmpSidecar(sidecarXmpMeta, false, null);

        var originalTag = CreatedOnLocalAndUtcFromXmpSidecarOriginalDateTime(sidecarXmpMeta);

        if (originalTag.createdOnUtc != null && photoLocation.HasValidLocation())
            return (TimeTools.LocalTimeFromUtcAndLocation(originalTag.createdOnUtc.Value, photoLocation.Latitude!.Value,
                photoLocation.Longitude!.Value), originalTag.createdOnUtc);

        if (originalTag.createdOnUtc != null && originalTag.createdOnLocal != null) return originalTag;

        var lastChanceUtc = gps.createdOnUtc ?? originalTag.createdOnUtc;
        var lastChanceLocal = gps.createdOnLocal ?? originalTag.createdOnLocal;

        return (lastChanceLocal, lastChanceUtc);
    }

    public static (DateTime? createdOnLocal, DateTime? createdOnUtc) CreatedOnLocalAndUtcFromXmpSidecarGpsTimeStamp(
        IXmpMeta? sidecarXmpMeta)
    {
        if (sidecarXmpMeta == null) return (null, null);

        var gpsDateTimeStamp = sidecarXmpMeta.GetPropertyDate("http://ns.adobe.com/exif/1.0/", "exif:GPSTimeStamp");

        if (gpsDateTimeStamp is not { HasDate: true }) return (null, null);

        var stampDateTime = new DateTime(gpsDateTimeStamp.Year, gpsDateTimeStamp.Month, gpsDateTimeStamp.Day,
            gpsDateTimeStamp.Hour, gpsDateTimeStamp.Minute, gpsDateTimeStamp.Second, DateTimeKind.Local);

        var utcDateTime = gpsDateTimeStamp.HasTimeZone ? stampDateTime.Add(gpsDateTimeStamp.Offset) : stampDateTime;

        utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);


        return (stampDateTime, utcDateTime);
    }

    public static (DateTime? createdOnLocal, DateTime? createdOnUtc) CreatedOnLocalAndUtcFromXmpSidecarOriginalDateTime(
        IXmpMeta? sidecarXmpMeta)
    {
        if (sidecarXmpMeta == null) return (null, null);

        var originalDateTime = sidecarXmpMeta.GetPropertyDate("http://ns.adobe.com/exif/1.0/", "exif:DateTimeOriginal");

        if (originalDateTime is not { HasDate: true }) return (null, null);

        var localTime = new DateTime(originalDateTime.Year, originalDateTime.Month, originalDateTime.Day,
            originalDateTime.Hour, originalDateTime.Minute, originalDateTime.Second, DateTimeKind.Local);

        if (!originalDateTime.HasTimeZone) return (localTime, null);

        var utcDateTime = localTime.Subtract(originalDateTime.Offset);
        utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return (localTime, utcDateTime);
    }


    public static double? LatitudeFromXmpSidecar(IXmpMeta? sidecarXmpMeta)
    {
        if (sidecarXmpMeta == null) return null;

        double? latitude = null;

        var gpsLatitudeProperty = sidecarXmpMeta.GetProperty("http://ns.adobe.com/exif/1.0/", "exif:GPSLatitude");

        if (string.IsNullOrWhiteSpace(gpsLatitudeProperty?.Value))
        {
            latitude = null;
        }
        else
        {
            var gpsLatitude = gpsLatitudeProperty.Value;
            var hemisphereModifier = gpsLatitude.Last().Equals('e') || gpsLatitude.Last().Equals('E') ? 1D : -1D;
            var parsed = double.TryParse(gpsLatitude[..^1], out var parsedLatitude);
            if (parsed) latitude = parsedLatitude * hemisphereModifier;
        }

        return latitude;
    }

    public static async Task<MetadataLocation> LocationFromXmpSidecar(
        IXmpMeta? sidecarXmpMeta, bool tryGetElevationIfNotInMetadata, IProgress<string>? progress)
    {
        var toReturn = new MetadataLocation
        {
            Latitude = LatitudeFromXmpSidecar(sidecarXmpMeta),
            Longitude = LongitudeFromXmpSidecar(sidecarXmpMeta),
            Elevation = AltitudeFromXmpSidecar(sidecarXmpMeta)
        };

        if (toReturn.Elevation == null && tryGetElevationIfNotInMetadata && toReturn.Latitude != null &&
            toReturn.Longitude != null)
            try
            {
                toReturn.Elevation = await ElevationService.OpenTopoNedElevation(toReturn.Latitude.Value,
                    toReturn.Longitude.Value, progress);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        return toReturn;
    }

    public static double? LongitudeFromXmpSidecar(IXmpMeta? sidecarXmpMeta)
    {
        if (sidecarXmpMeta == null) return null;

        double? longitude = null;

        var gpsLongitudeProperty = sidecarXmpMeta.GetProperty("http://ns.adobe.com/exif/1.0/", "exif:GPSLongitude");

        if (string.IsNullOrWhiteSpace(gpsLongitudeProperty?.Value))
        {
            longitude = null;
        }
        else
        {
            var gpsLongitude = gpsLongitudeProperty.Value;
            var eastWestModifier = gpsLongitude.Last().Equals('e') || gpsLongitude.Last().Equals('E') ? 1 : -1;
            var parsed = double.TryParse(gpsLongitude[..^1], out var parsedLongitude);
            if (parsed) longitude = parsedLongitude * eastWestModifier;
        }

        return longitude;
    }
}