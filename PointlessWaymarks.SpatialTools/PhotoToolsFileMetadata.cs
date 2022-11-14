using System.Globalization;
using GeoTimeZone;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;

namespace PointlessWaymarks.SpatialTools;

public static class PhotoToolsFileMetadata
{
    public static async Task<(DateTime? createdOnLocal, DateTime? createdOnUtc)> CreatedOnLocalAndUtc(
        ExifSubIfdDirectory? exifSubIfDirectory, ExifIfd0Directory? exifIfdDirectory, GpsDirectory? gpsDirectory,
        XmpDirectory? xmpDirectory)
    {
        var gps = await CreatedOnLocalAndUtcFromGps(gpsDirectory);

        if (gps.createdOnUtc != null && gps.createdOnLocal != null) return gps;

        var photoLocation = await LocationFromExifGpsDirectoryMetadata(gpsDirectory, false, null);

        var exif = CreatedOnLocalAndUtcFromExif(exifSubIfDirectory, exifIfdDirectory);

        if (exif.createdOnUtc != null && photoLocation.HasValidLocation())
            return (TimeTools.LocalTimeFromUtcAndLocation(exif.createdOnUtc.Value, photoLocation.Latitude!.Value,
                photoLocation.Longitude!.Value), exif.createdOnUtc);

        if (exif.createdOnUtc != null && exif.createdOnLocal != null) return exif;

        var xmp = CreatedOnLocalAndUtcFromXmp(xmpDirectory);

        if (xmp.createdOnUtc != null && photoLocation.HasValidLocation())
            return (
                TimeTools.LocalTimeFromUtcAndLocation(xmp.createdOnUtc.Value, photoLocation.Latitude!.Value,
                    photoLocation.Longitude!.Value), xmp.createdOnUtc);

        if (xmp.createdOnUtc != null && xmp.createdOnLocal != null) return xmp;

        var lastChanceUtc = gps.createdOnUtc ?? exif.createdOnUtc ?? xmp.createdOnUtc;
        var lastChanceLocal = gps.createdOnLocal ?? exif.createdOnLocal ?? xmp.createdOnLocal;

        return (lastChanceLocal, lastChanceUtc);
    }

    public static (DateTime? createdOnLocal, DateTime? createdOnUtc) CreatedOnLocalAndUtcFromExif(
        ExifSubIfdDirectory? exifSubIfDirectory, ExifIfd0Directory? exifIfdDirectory)
    {
        var localTime = CreateOnLocalDateTimeFromExif(exifSubIfDirectory, exifIfdDirectory);

        if (localTime == null) return (null, null);

        localTime = DateTime.SpecifyKind(localTime.Value, DateTimeKind.Local);

        var offset = CreatedOnUtcOffsetFromExif(exifSubIfDirectory);

        if (!offset.validTimeZoneOffset) return (localTime, null);

        var utcDateTime = localTime.Value.Subtract(offset.offset);
        utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return (localTime, utcDateTime);
    }

    public static async Task<(DateTime? createdOnLocal, DateTime? createdOnUtc)> CreatedOnLocalAndUtcFromGps(
        GpsDirectory? gpsDirectory)
    {
        if (gpsDirectory == null) return (null, null);

        var hasGpsTime = gpsDirectory.TryGetGpsDate(out var gpsDateTime);

        if (!hasGpsTime || gpsDateTime == DateTime.MinValue) return (null, null);

        gpsDateTime = DateTime.SpecifyKind(gpsDateTime, DateTimeKind.Utc);

        var gpsLocation = await LocationFromExifGpsDirectoryMetadata(gpsDirectory, false, null);

        if (!gpsLocation.HasValidLocation()) return (null, gpsDateTime);

        var photoLocationTimezoneIanaIdentifier =
            TimeZoneLookup.GetTimeZone(gpsLocation.Latitude!.Value, gpsLocation.Longitude!.Value);
        var photoLocationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(photoLocationTimezoneIanaIdentifier.Result);
        var localTime = TimeZoneInfo.ConvertTime(gpsDateTime, photoLocationTimeZone);

        return (localTime, gpsDateTime);
    }

    public static (DateTime? createdOnLocal, DateTime? createdOnUtc) CreatedOnLocalAndUtcFromXmp(
        XmpDirectory? xmpDirectory)
    {
        if (xmpDirectory == null) return (null, null);

        var xmpDateTimeOriginals = xmpDirectory.GetXmpProperties()
            .Where(x => x.Key.Equals("DateTimeOriginal", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var loopDto in xmpDateTimeOriginals)
        {
            var parsed = DateTimeOffset.TryParseExact(loopDto.Value, "yyyy-MM-ddTHH:mm:ss.ffzzz",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedValue);
            if (!parsed)
                parsed = DateTimeOffset.TryParseExact(loopDto.Value, "yyyy-MM-ddTHH:mm:sszzz",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedValue);

            if (parsed) return (parsedValue.DateTime, parsedValue.UtcDateTime);
        }

        foreach (var loopDto in xmpDateTimeOriginals)
        {
            var parsed = DateTime.TryParseExact(loopDto.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedValue);
            if (!parsed)
                parsed = DateTime.TryParseExact(loopDto.Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out parsedValue);

            if (parsed) return (parsedValue, null);
        }

        return (null, null);
    }

    private static (bool validTimeZoneOffset, TimeSpan offset) CreatedOnUtcOffsetFromExif(
        ExifSubIfdDirectory? exifSubIfDirectory)
    {
        if (exifSubIfDirectory == null) return (false, TimeSpan.Zero);

        var createdOnUtcOffsetString = exifSubIfDirectory.GetString(ExifDirectoryBase.TagTimeZone);
        var createOnUtcOffsetTimespan = new TimeSpan(0);

        if (string.IsNullOrWhiteSpace(createdOnUtcOffsetString)) return (false, createOnUtcOffsetTimespan);

        var createOnUtcOffsetIsValid = TimeSpan.TryParse(createdOnUtcOffsetString, out createOnUtcOffsetTimespan);

        if (createOnUtcOffsetIsValid) return (createOnUtcOffsetIsValid, createOnUtcOffsetTimespan);

        //This is 0x882a - TimeZoneOffset - the Exif Tags documentation lists this as an Int16[] with the 2nd value 
        //referring to an offset for the ModifyDate if present. In testing against the MetadataExtractor images I
        //could only find single values so using that here...
        var translatedTimeZoneOffset =
            exifSubIfDirectory.TryGetInt16(ExifDirectoryBase.TagTimeZoneOffset, out var offsetInHours);

        if (!translatedTimeZoneOffset) return (false, TimeSpan.Zero);

        return (true, new TimeSpan(offsetInHours));
    }

    private static DateTime? CreateOnLocalDateTimeFromExif(ExifSubIfdDirectory? exifSubIfDirectory,
        ExifIfd0Directory? exifIfdDirectory)
    {
        var createdOn = DateTime.MinValue;

        var succeeded = exifSubIfDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out createdOn);

        //Largely anecdotal on order here - more research could probably improve this...
        if (!succeeded ?? false)
            succeeded = exifIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out createdOn);
        if (!succeeded ?? false)
            succeeded = exifSubIfDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out createdOn);
        if (!succeeded ?? false)
            succeeded = exifIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out createdOn);

        if (succeeded ?? false) return createdOn;

        return null;
    }

    public static async Task<PhotoLocation> LocationFromExifGpsDirectoryMetadata(GpsDirectory? gpsDirectory,
        bool tryGetElevationIfNotInMetadata, IProgress<string>? progress)
    {
        var toReturn = new PhotoLocation();

        if (gpsDirectory is null || gpsDirectory.IsEmpty) return toReturn;
        var geoLocation = gpsDirectory.GetGeoLocation();

        if (geoLocation?.IsZero ?? true)
        {
            toReturn.Longitude = null;
            toReturn.Latitude = null;
        }
        else
        {
            toReturn.Latitude = geoLocation.Latitude;
            toReturn.Longitude = geoLocation.Longitude;
        }

        var hasSeaLevelIndicator =
            gpsDirectory.TryGetByte(GpsDirectory.TagAltitudeRef, out var seaLevelIndicator);

        var hasElevation = gpsDirectory.TryGetRational(GpsDirectory.TagAltitude, out var altitudeRational);

        if (hasElevation)
        {
            var isBelowSeaLevel = false;

            if (hasSeaLevelIndicator) isBelowSeaLevel = seaLevelIndicator == 1;

            if (altitudeRational.Denominator != 0 ||
                (altitudeRational.Denominator == 0 && altitudeRational.Numerator == 0))
                toReturn.Elevation = isBelowSeaLevel
                    ? altitudeRational.ToDouble() * -1
                    : altitudeRational.ToDouble();
        }

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

    public static async Task<PhotoLocation> LocationFromFile(FileInfo fileInfo,
        bool tryGetElevationIfNotInMetadata, IProgress<string>? progress)
    {
        var gpsDirectory = ImageMetadataReader.ReadMetadata(fileInfo.FullName).OfType<GpsDirectory>()
            .FirstOrDefault();

        return await LocationFromExifGpsDirectoryMetadata(gpsDirectory, tryGetElevationIfNotInMetadata, progress);
    }
}