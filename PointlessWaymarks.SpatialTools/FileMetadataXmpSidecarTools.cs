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

        var altitudeIsAboveSeaLevel = string.IsNullOrWhiteSpace(gpsAltitudeRef?.Value) || gpsAltitudeRef.Value.Equals("0");

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

        if (gps is { createdOnUtc: { }, createdOnLocal: { } }) return gps;

        var photoLocation = await LocationFromXmpSidecar(sidecarXmpMeta, false, null);

        var originalTag = CreatedOnLocalAndUtcFromXmpSidecarOriginalDateTime(sidecarXmpMeta);

        if (originalTag.createdOnUtc != null && photoLocation.HasValidLocation())
            return (TimeTools.LocalTimeFromUtcAndLocation(originalTag.createdOnUtc.Value, photoLocation.Latitude!.Value,
                photoLocation.Longitude!.Value), originalTag.createdOnUtc);

        if (originalTag is { createdOnUtc: { }, createdOnLocal: { } }) return originalTag;

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

    public static List<string> KeywordsFromXmpSidecar(IXmpMeta? sidecarXmpMeta, bool splitOnCommaAndSemiColon)
    {
        if (sidecarXmpMeta == null) return [];

        var extractedKeywords = new List<string>();

        var arrayItemCount = sidecarXmpMeta.CountArrayItems("http://purl.org/dc/elements/1.1/", "dc:subject");

        for (var i = 1; i <= arrayItemCount; i++)
            extractedKeywords.Add(
                sidecarXmpMeta.GetArrayItem("http://purl.org/dc/elements/1.1/", "dc:subject", i).Value);

        if (splitOnCommaAndSemiColon)
            extractedKeywords = extractedKeywords.SelectMany(x => x.Replace(";", ",").Split(",")).ToList();

        extractedKeywords = extractedKeywords.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).ToList();

        extractedKeywords = extractedKeywords.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        return extractedKeywords;
    }


    public static double? LatitudeFromXmpSidecar(IXmpMeta? sidecarXmpMeta)
    {
        if (sidecarXmpMeta == null) return null;

        var gpsLatitudeProperty = sidecarXmpMeta.GetProperty("http://ns.adobe.com/exif/1.0/", "exif:GPSLatitude");

        if (string.IsNullOrWhiteSpace(gpsLatitudeProperty?.Value)) return null;

        var gpsLatitude = gpsLatitudeProperty.Value;
        var hemisphereModifier = gpsLatitude.Last().Equals('n') || gpsLatitude.Last().Equals('N') ? 1D : -1D;
        var latitudeParts = gpsLatitude[..^1].Split(",");

        if (latitudeParts.Length != 2) return null;

        var degreesParsed = int.TryParse(latitudeParts[0], out var degrees);
        var minutesParsed = double.TryParse(latitudeParts[1], out var minutes);

        if (!degreesParsed || !minutesParsed) return null;

        return hemisphereModifier * (degrees + minutes / 60D);
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

        if (toReturn.Elevation == null && tryGetElevationIfNotInMetadata && toReturn is { Latitude: { }, Longitude: { } })
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

        var gpsLongitudeProperty = sidecarXmpMeta.GetProperty("http://ns.adobe.com/exif/1.0/", "exif:GPSLongitude");

        if (string.IsNullOrWhiteSpace(gpsLongitudeProperty?.Value)) return null;

        var gpsLongitude = gpsLongitudeProperty.Value;
        var eastWestModifier = gpsLongitude.Last().Equals('e') || gpsLongitude.Last().Equals('E') ? 1 : -1;

        var longitudeParts = gpsLongitude[..^1].Split(",");

        if (longitudeParts.Length != 2) return null;

        var degreesParsed = int.TryParse(longitudeParts[0], out var degrees);
        var minutesParsed = double.TryParse(longitudeParts[1], out var minutes);

        if (!degreesParsed || !minutesParsed) return null;

        return eastWestModifier * (degrees + minutes / 60D);
    }
}