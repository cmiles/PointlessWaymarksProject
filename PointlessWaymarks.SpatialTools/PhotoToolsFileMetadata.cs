using System.Globalization;
using GeoTimeZone;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;

namespace PointlessWaymarks.SpatialTools;

public static class PhotoToolsFileMetadata
{
    public static async Task<(DateTime? createdOnLocal, DateTime? createdOnUtc)> CreatedOnLocalAndUtc(
        IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var gpsLocal = await CreatedOnLocalFromGps(directories);
        var gpsUtc = await CreatedOnUtcFromGps(directories);

        if (gpsLocal != null && gpsUtc != null) return (gpsLocal, gpsUtc);

        var photoLocation = await LocationFromExif(directories, false, null);

        var exifLocal = CreatedOnLocalFromExif(directories);
        var exifUtc = CreatedOnUtcFromExif(directories);

        if (exifUtc != null && photoLocation.HasValidLocation())
            return (TimeTools.LocalTimeFromUtcAndLocation(exifUtc.Value, photoLocation.Latitude!.Value,
                photoLocation.Longitude!.Value), exifUtc);

        if (exifUtc != null && exifLocal != null) return (exifLocal, exifUtc);

        var xmpLocal = CreatedOnLocalFromXmp(directories);
        var xmpUtc = CreatedOnUtcFromXmp(directories);

        if (xmpUtc != null && photoLocation.HasValidLocation())
            return (
                TimeTools.LocalTimeFromUtcAndLocation(xmpUtc.Value, photoLocation.Latitude!.Value,
                    photoLocation.Longitude!.Value), xmpUtc);

        if (xmpUtc != null && xmpLocal != null) return (xmpLocal, xmpLocal);

        var lastChanceUtc = gpsUtc ?? exifUtc ?? xmpUtc;
        var lastChanceLocal = gpsLocal ?? exifLocal ?? xmpLocal;

        return (lastChanceLocal, lastChanceUtc);
    }

    private static DateTime? CreatedOnUtcFromExif(
        IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var localTime = CreatedOnLocalFromExif(directories);

        if (localTime == null) return null;

        localTime = DateTime.SpecifyKind(localTime.Value, DateTimeKind.Local);

        var offset = CreatedOnUtcOffsetFromExif(directories);

        if (!offset.validTimeZoneOffset) return null;

        var utcDateTime = localTime.Value.Subtract(offset.offset);
        utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return utcDateTime;
    }

    public static async Task<DateTime?> CreatedOnLocalFromGps(
        IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var allGpsDirectories = directories.OfType<GpsDirectory>().ToList();

        foreach (var loopDirectory in allGpsDirectories)
        {
            var result = await CreatedOnLocalFromGpsMetadata(loopDirectory);
            if(result != null) return result;
        }

        return null;
    }

    public static async Task<DateTime?> CreatedOnUtcFromGps(
        IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var allGpsDirectories = directories.OfType<GpsDirectory>().ToList();

        foreach (var loopDirectory in allGpsDirectories)
        {
            var result = await CreatedOnUtcFromGpsMetadata(loopDirectory);
            if (result != null) return result;
        }

        return null;
    }

    private static async Task<DateTime?> CreatedOnLocalFromGpsMetadata(
        GpsDirectory? gpsDirectory)
    {
        if (gpsDirectory == null) return null;

        var hasGpsTime = gpsDirectory.TryGetGpsDate(out var gpsDateTime);

        if (!hasGpsTime || gpsDateTime == DateTime.MinValue) return null;

        gpsDateTime = DateTime.SpecifyKind(gpsDateTime, DateTimeKind.Utc);

        var gpsLocation = await LocationFromExifGpsMetadata(gpsDirectory, false, null);

        if (!gpsLocation.HasValidLocation()) return null;

        var photoLocationTimezoneIanaIdentifier =
            TimeZoneLookup.GetTimeZone(gpsLocation.Latitude!.Value, gpsLocation.Longitude!.Value);
        var photoLocationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(photoLocationTimezoneIanaIdentifier.Result);
        var localTime = TimeZoneInfo.ConvertTime(gpsDateTime, photoLocationTimeZone);

        return localTime;
    }

    private static async Task<DateTime?> CreatedOnUtcFromGpsMetadata(
        GpsDirectory? gpsDirectory)
    {
        if (gpsDirectory == null) return null;

        var hasGpsTime = gpsDirectory.TryGetGpsDate(out var gpsDateTime);

        if (!hasGpsTime || gpsDateTime == DateTime.MinValue) return null;

        gpsDateTime = DateTime.SpecifyKind(gpsDateTime, DateTimeKind.Utc);

        var gpsLocation = await LocationFromExifGpsMetadata(gpsDirectory, false, null);

        if (!gpsLocation.HasValidLocation()) return null;

        var photoLocationTimezoneIanaIdentifier =
            TimeZoneLookup.GetTimeZone(gpsLocation.Latitude!.Value, gpsLocation.Longitude!.Value);
        var photoLocationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(photoLocationTimezoneIanaIdentifier.Result);
        var localTime = TimeZoneInfo.ConvertTime(gpsDateTime, photoLocationTimeZone);

        return gpsDateTime;
    }

    public static DateTime? CreatedOnLocalFromXmp(
        IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var xmpDirectories = directories.OfType<XmpDirectory>().ToList();

        var resultList = new List<(DateTime? createdOnLocal, DateTime? createdOnUtc)>();

        foreach (var loopDirectory in xmpDirectories)
        {
            var result = CreatedOnLocalFromXmpMetadata(loopDirectory);
            if (result != null) return result;
        }

        return null;
    }

    public static DateTime? CreatedOnUtcFromXmp(
        IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var xmpDirectories = directories.OfType<XmpDirectory>().ToList();

        var resultList = new List<(DateTime? createdOnLocal, DateTime? createdOnUtc)>();

        foreach (var loopDirectory in xmpDirectories)
        {
            var result = CreatedOnUtcFromXmpMetadata(loopDirectory);
            if (result != null) return result;
        }

        return null;
    }

    private static DateTime? CreatedOnLocalFromXmpMetadata(
        XmpDirectory? xmpDirectory)
    {
        if (xmpDirectory == null) return null;

        var xmpDateTimeOriginals = xmpDirectory.GetXmpProperties()
            .Where(x => x.Key.Equals("DateTimeOriginal", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var loopDto in xmpDateTimeOriginals)
        {
            var parsed = DateTimeOffset.TryParseExact(loopDto.Value, "yyyy-MM-ddTHH:mm:ss.ffzzz",
                CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedValue);
            if (!parsed)
                parsed = DateTimeOffset.TryParseExact(loopDto.Value, "yyyy-MM-ddTHH:mm:sszzz",
                    CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedValue);

            if (parsed) return parsedValue.DateTime;
        }

        foreach (var loopDto in xmpDateTimeOriginals)
        {
            var parsed = DateTime.TryParseExact(loopDto.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var parsedValue);
            if (!parsed)
                parsed = DateTime.TryParseExact(loopDto.Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out parsedValue);

            if (parsed) return parsedValue;
        }

        return null;
    }

    private static DateTime? CreatedOnUtcFromXmpMetadata(
        XmpDirectory? xmpDirectory)
    {
        if (xmpDirectory == null) return null;

        var xmpDateTimeOriginals = xmpDirectory.GetXmpProperties()
            .Where(x => x.Key.Equals("DateTimeOriginal", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var loopDto in xmpDateTimeOriginals)
        {
            var parsed = DateTimeOffset.TryParseExact(loopDto.Value, "yyyy-MM-ddTHH:mm:ss.ffzzz",
                CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedValue);
            if (!parsed)
                parsed = DateTimeOffset.TryParseExact(loopDto.Value, "yyyy-MM-ddTHH:mm:sszzz",
                    CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedValue);

            if (parsed) return parsedValue.UtcDateTime;
        }

        return null;
    }

    public static (bool validTimeZoneOffset, TimeSpan offset) CreatedOnUtcOffsetFromExif(
        IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var subIfDirectories = directories.OfType<ExifSubIfdDirectory>().ToList();

        foreach (var loopSubIf in subIfDirectories)
        {
            var result = CreatedOnUtcOffsetFromExifSubIfdMetadata(loopSubIf);
            if (result.validTimeZoneOffset) return result;
        }

        return (false, TimeSpan.Zero);
    }

    private static (bool validTimeZoneOffset, TimeSpan offset) CreatedOnUtcOffsetFromExifSubIfdMetadata(
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

    public static DateTime? CreatedOnLocalFromExif(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var subIfdDirectories = directories.OfType<ExifSubIfdDirectory>().ToList();

        foreach (var loopSubIf in subIfdDirectories)
        {
            var result = CreateOnLocalFromExifSubIfdMetadata(loopSubIf);
            if (result != null) return result;
        }

        var ifdDirectories = directories.OfType<ExifIfd0Directory>().ToList();

        foreach (var loopIfd in ifdDirectories)
        {
            var result = CreateOnLocalDateTimeFromExifIfdMetadata(loopIfd);
            if (result != null) return result;
        }

        return null;
    }

    private static DateTime? CreateOnLocalFromExifSubIfdMetadata(ExifSubIfdDirectory? exifSubIfDirectory)
    {
        var createdOn = DateTime.MinValue;

        var succeeded = exifSubIfDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out createdOn);

        if (!succeeded ?? false)
            succeeded = exifSubIfDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out createdOn);

        if (succeeded ?? false) return createdOn;

        return null;
    }

    private static DateTime? CreateOnLocalDateTimeFromExifIfdMetadata(ExifIfd0Directory? exifIfdDirectory)
    {
        var createdOn = DateTime.MinValue;

        var succeeded = exifIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out createdOn);

        if (!succeeded ?? false)
            succeeded = exifIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out createdOn);

        if (succeeded ?? false) return createdOn;

        return null;
    }

    public static async Task<PhotoLocation> LocationFromExif(
        IReadOnlyList<MetadataExtractor.Directory> directories, bool tryGetElevationIfNotInMetadata,
        IProgress<string>? progress)
    {
        var allGpsDirectories = directories.OfType<GpsDirectory>().ToList();

        foreach (var loopGpsDirectory in allGpsDirectories)
        {
            var location =
                await LocationFromExifGpsMetadata(loopGpsDirectory, tryGetElevationIfNotInMetadata, progress);

            if (location.HasValidLocation()) return location;
        }

        return new PhotoLocation();
    }

    private static async Task<PhotoLocation> LocationFromExifGpsMetadata(GpsDirectory? gpsDirectory,
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
}