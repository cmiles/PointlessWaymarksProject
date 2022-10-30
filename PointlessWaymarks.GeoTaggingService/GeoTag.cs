﻿using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.SpatialTools;
using Serilog;
using File = TagLib.File;

namespace PointlessWaymarks.GeoTaggingService;

public class GeoTag
{
    /// <summary>
    ///     From https://exiftool.org/exiftool_pod.html on 10/30/2022 with processing and manual filtering
    ///     in Excel to pick 'File Types' from the list with r/w or r/w/c support. .JPG added in addition to .JPEG.
    /// </summary>
    public List<string> ExifToolWriteSupportedExtensions => new List<string>
    {
        ".360",
        ".3G2",
        ".3GP",
        ".AAX",
        ".AI",
        ".ARQ",
        ".ARW",
        ".AVIF",
        ".CR2",
        ".CR3",
        ".CRM",
        ".CRW",
        ".CS1",
        ".DCP",
        ".DNG",
        ".DR4",
        ".DVB",
        ".EPS",
        ".ERF",
        ".EXIF",
        ".EXV",
        ".F4A/V",
        ".FFF",
        ".FLIF",
        ".GIF",
        ".GPR",
        ".HDP",
        ".HEIC",
        ".HEIF",
        ".ICC",
        ".IIQ",
        ".IND",
        ".INSP",
        ".JNG",
        ".JP2",
        ".JPEG",
        ".LRV",
        ".M4A/V",
        ".MEF",
        ".MIE",
        ".MNG",
        ".MOS",
        ".MOV",
        ".MP4",
        ".MPO",
        ".MQV",
        ".MRW",
        ".NEF",
        ".NKSC",
        ".NRW",
        ".ORF",
        ".ORI",
        ".PBM",
        ".PDF",
        ".PEF",
        ".PGM",
        ".PNG",
        ".PPM",
        ".PS",
        ".PSB",
        ".PSD",
        ".QTIF",
        ".RAF",
        ".RAW",
        ".RW2",
        ".RWL",
        ".SR2",
        ".SRW",
        ".THM",
        ".TIFF",
        ".VRD",
        ".WDP",
        ".WEBP",
        ".X3F",
        ".XMP"
    }.Select(x => x.ToUpperInvariant()).OrderBy(x => x).ToList();

    /// <summary>
    ///     From the supported Images list on https://github.com/mono/taglib-sharp on 10/30/2022.
    ///     Dots, upper case and JPG manually changed/added.
    /// </summary>
    public List<string> TagSharpSupportedExtensions => new List<string>
    {
        ".BMP", ".GIF", ".JPEG", ".JPG", ".PBM", ".PGM", ".PPM", ".PNM", ".PCX", ".PNG", ".TIFF", ".DNG", ".SVG"
    }.Select(x => x.ToUpperInvariant()).OrderBy(x => x).ToList();

    public static (bool isPresent, FileInfo? exifToolFile) ExifTool(string? exifToolDirectory)
    {
        if (string.IsNullOrEmpty(exifToolDirectory)) return (false, null);

        var possibleDirectory = new DirectoryInfo(exifToolDirectory);

        if (!possibleDirectory.Exists) return (false, null);

        var possibleExifToolFile = new FileInfo(Path.Combine(possibleDirectory.FullName, "ExifTool.exe"));

        if (!possibleExifToolFile.Exists) return (false, null);

        return (false, possibleExifToolFile);
    }

    public static bool FileHasLatLong(FileInfo loopFile, IProgress<string>? progress)
    {
        var gpsDirectory = ImageMetadataReader.ReadMetadata(loopFile.FullName).OfType<GpsDirectory>()
            .FirstOrDefault();

        if (gpsDirectory is { IsEmpty: false }) return false;

        var geoLocation = gpsDirectory.GetGeoLocation();

        if (geoLocation?.IsZero ?? true) return false;

        return true;
    }

    public static DateTime? FileUtcCreatedOn(FileInfo loopFile, IProgress<string>? progress)
    {
        var exifSubIfDirectory = ImageMetadataReader.ReadMetadata(loopFile.FullName)
            .OfType<ExifSubIfdDirectory>()
            .FirstOrDefault();
        var gpsDirectory = ImageMetadataReader.ReadMetadata(loopFile.FullName).OfType<GpsDirectory>()
            .FirstOrDefault();

        if (gpsDirectory?.TryGetGpsDate(out var gpsDateTime) ?? false)
            if (gpsDateTime != DateTime.MinValue)
            {
                gpsDateTime = DateTime.SpecifyKind(gpsDateTime, DateTimeKind.Utc);
                progress?.Report($"GeoTag - {loopFile.FullName} using GPS UTC Time {gpsDateTime}");
                return gpsDateTime;
            }

        var createdOn = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

        if (string.IsNullOrEmpty(createdOn))
        {
            progress?.Report(
                $"GeoTag - No TagDateTimeOriginal found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");
            return null;
        }

        var createdOnParsed = DateTime.TryParseExact(createdOn, "yyyy:MM:dd HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate);

        if (!createdOnParsed)
        {
            progress?.Report(
                $"GeoTag - TagDateTimeOriginal - {createdOn} - could not be parsed into a valid DateTime - skipping");
            return null;
        }

        var createdOnUtcOffsetString = exifSubIfDirectory?.GetDescription(ExifDirectoryBase.TagTimeZone);
        var createOnUtcOffsetTimespan = new TimeSpan(0);
        var createOnUtcOffsetIsValid = !string.IsNullOrWhiteSpace(createdOnUtcOffsetString) &&
                                       TimeSpan.TryParse(createdOnUtcOffsetString,
                                           out createOnUtcOffsetTimespan);

        if (!createOnUtcOffsetIsValid)
        {
            progress?.Report(
                $"GeoTag - Valid TagTimeZone not found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");
            return null;
        }

        return parsedDate.Subtract(createOnUtcOffsetTimespan);
    }

    public async System.Threading.Tasks.Task Tag(List<FileInfo> filesToTag, List<IGpxService> gpxServices,
        bool createBackupBeforeWritingMetadata, int pointMustBeWithinMinutes, int adjustCreatedTimeInMinutes,
        bool overwriteExistingLatLong, string? exifToolDirectory = null, IProgress<string>? progress = null)
    {
        Log.Information(
            $"GeoTag - Starting - {filesToTag.Count} Files, {gpxServices.Count} GpxServices, Create Backup {createBackupBeforeWritingMetadata}, {pointMustBeWithinMinutes} Within Minutes, {adjustCreatedTimeInMinutes} Adjustment, Overwrite {overwriteExistingLatLong}");

        if (!createBackupBeforeWritingMetadata)
            progress?.Report(
                "WARNING - writing metadata into files is never going to be 100% without errors - this method is running without creating backups ('in place updates only') - in the future consider allowing backups to be created to avoid any unfortunate incidents where metadata additions result in corrupted files!");

        if (!filesToTag.Any())
        {
            progress?.Report("GeoTag - No files to tag, ending...");
            return;
        }

        if (!gpxServices.Any())
        {
            progress?.Report("GeoTag - No GPX Services to tag with, ending...");
            return;
        }

        var exifTool = ExifTool(exifToolDirectory);

        var supportedFileExtensions = exifTool.isPresent
            ? TagSharpSupportedExtensions.Union(ExifToolWriteSupportedExtensions).ToList()
            : TagSharpSupportedExtensions;

        var supportedFiles = new List<FileInfo>();
        var notSupportedFiles = new List<FileInfo>();

        foreach (var loopFile in filesToTag)
        {
            var isSupported = supportedFileExtensions.Contains(loopFile.Extension.ToUpperInvariant());

            if (isSupported) supportedFiles.Add(loopFile);
            else notSupportedFiles.Add(loopFile);
        }

        if (notSupportedFiles.Any())
        {
            var notSupportedFileExtensions = string.Join(", ",
                supportedFiles.Select(x => x.Extension.ToUpperInvariant()).Distinct().OrderBy(x => x));

            progress?.Report(
                $"GeoTag - Found {notSupportedFiles.Count} with extensions that are not in this program's  of supported extensions {(exifTool.isPresent ? "" : "(ExifTool was not found and expands considerably the supported file extensions) ")}.");

            progress?.Report(
                $"GeoTag - {notSupportedFileExtensions.Length} Extensions without support: {notSupportedFileExtensions}");

            progress?.Report(
                $"GeoTag - {notSupportedFiles.Count} Files without support: {string.Join(", ", notSupportedFiles.Select(x => x.FullName).OrderBy(x => x))}");
        }

        progress?.Report($"GeoTag - {supportedFiles.Count} Supported Files");

        var listOfUtcAndFileToProcess = new List<(DateTime createdUtc, FileInfo file)>();

        progress?.Report("GeoTag - Getting photo UTC Time (required) and checking for existing Lat/Long");
        var counter = 0;

        foreach (var loopFile in supportedFiles)
        {
            if (++counter % 50 == 0)
                progress?.Report($"GeoTag - Processing Metadata - File {counter} of {supportedFiles.Count}");

            if (!loopFile.Exists)
            {
                progress?.Report($"GeoTag - File {loopFile.FullName} doesn't exist - skipping");
                return;
            }

            var hasExistingLatLong = FileHasLatLong(loopFile, progress);

            if (!overwriteExistingLatLong && hasExistingLatLong)
            {
                progress?.Report(
                    $"GeoTag - File {loopFile.FullName} Already Has a GeoLocation and Overwrite Existing is Set to {overwriteExistingLatLong} - skipping");
                continue;
            }

            var createdOnUtc = FileUtcCreatedOn(loopFile, progress);

            if (createdOnUtc == null)
            {
                progress?.Report(
                    $"GeoTag - Valid TagTimeZone not found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");
                continue;
            }

            listOfUtcAndFileToProcess.Add((createdOnUtc.Value.AddMinutes(adjustCreatedTimeInMinutes), loopFile));
        }

        progress?.Report(
            $"Found {listOfUtcAndFileToProcess.Count} files to Process out of {supportedFiles.Count} supported Files");

        var pointLists = new List<(DateTime start, DateTime end, List<GpxWaypoint> waypoints)>();

        counter = 0;

        foreach (var loopUtc in listOfUtcAndFileToProcess)
        {
            if (++counter % 50 == 0)
                progress?.Report($"GeoTag - Getting Points - File {counter} of {listOfUtcAndFileToProcess.Count}");

            if (pointLists.Any(x => loopUtc.createdUtc >= x.start && loopUtc.createdUtc <= x.end))
            {
                progress?.Report(
                    $"GeoTag - {loopUtc.file} has a UTC Time of {loopUtc.createdUtc}, points for this time have already been added - continuing");
                continue;
            }

            var pointsCollection = new List<GpxWaypoint>();

            foreach (var loopService in gpxServices)
                pointsCollection.AddRange(
                    (await loopService.GetGpxTrack(loopUtc.createdUtc)).Where(x => x.TimestampUtc != null));

            var toAdd = (pointsCollection.MinBy(x => x.TimestampUtc!.Value).TimestampUtc.Value,
                pointsCollection.MaxBy(x => x.TimestampUtc!.Value).TimestampUtc.Value, pointsCollection);

            progress?.Report(
                $"GeoTag - For {loopUtc.file} added {toAdd.pointsCollection.Count} points from UTC {toAdd.Item1} to {toAdd.Item2}");

            pointLists.Add(toAdd);
        }

        var allPoints = pointLists.SelectMany(x => x.waypoints).ToList();

        progress?.Report(
            $"Found a total of {allPoints.Count} points to check for files within {adjustCreatedTimeInMinutes} minutes of for location tagging");

        counter = 0;

        foreach (var loopFile in listOfUtcAndFileToProcess)
        {
            if (++counter % 50 == 0)
                progress?.Report(
                    $"GeoTag - Finding Location and Writing Metadata - File {counter} of {listOfUtcAndFileToProcess.Count}");

            var possibleTagPoints = allPoints.Where(x =>
                loopFile.createdUtc.AddMinutes(-Math.Abs(pointMustBeWithinMinutes)) >= x.TimestampUtc &&
                x.TimestampUtc <= loopFile.createdUtc.AddMinutes(Math.Abs(pointMustBeWithinMinutes))).ToList();

            if (!possibleTagPoints.Any())
            {
                progress?.Report(
                    $"GeoTag - No Matching Points Found for {loopFile.file.FullName}");
                continue;
            }

            var closest = possibleTagPoints.MinBy(x =>
                Math.Abs(loopFile.createdUtc.Subtract(x.TimestampUtc.Value).TotalMicroseconds));

            var latitude = closest.Latitude.Value;
            var longitude = closest.Longitude.Value;
            double? elevation = null;
            if (closest.ElevationInMeters is not null) elevation = closest.ElevationInMeters;
            else
                try
                {
                    elevation = await ElevationService.OpenTopoNedElevation(closest.Latitude, closest.Longitude,
                        progress);
                }
                catch (Exception e)
                {
                    Log.Verbose("GeoTag - Failed to get Elevation, Silent Error");
                }

            progress?.Report(
                $"GeoTag - For {loopFile.file.FullName} found Lat: {latitude} Long: {longitude} Elevation {elevation}");


            if (createBackupBeforeWritingMetadata)
            {
                var backUpSuccessful = WriteFileToBackupDirectory(loopFile.file, progress);
                if (!backUpSuccessful)
                    progress?.Report(
                        $"GeoTag - Skipping {loopFile.file.FullName} - Found GeoTag information but could not create a backup - skipping this file.");
            }

            if (TagSharpSupportedExtensions.Contains(loopFile.file.Extension))
                if (File.Create(loopFile.file.FullName) is TagLib.Image.File tagSharpFile)
                {
                    tagSharpFile.ImageTag.Latitude = latitude;
                    tagSharpFile.ImageTag.Longitude = longitude;
                    if (elevation is not null) tagSharpFile.ImageTag.Altitude = elevation;
                    tagSharpFile.Save();
                    progress?.Report("GeoTag - Wrote Metadata with TagSharp");
                    continue;
                }

            var exifToolParameters = elevation is null
                ? $"-GPSLatitude*={latitude} -GPSLongitude*=-{longitude} \"{loopFile.file.FullName}\""
                : $"-GPSLatitude*={latitude} -GPSLongitude*=-{longitude} -GPSAltitude*={elevation} \"{loopFile.file.FullName}\"";
            try
            {
                ProcessRunner.Execute(exifTool.exifToolFile.FullName, exifToolParameters, progress);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            progress?.Report(
                $"GeoTag - Wrote Metadata with ExifTool - {exifTool.exifToolFile.FullName} {exifToolParameters}");
        }
    }

    public static bool WriteFileToBackupDirectory(FileInfo fileToBackup, IProgress<string>? progress)
    {
        var directoryInfo = new DirectoryInfo(fileToBackup.DirectoryName ?? string.Empty);

        var backupDirectory = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "PwGeoTagBackup"));
        if (!backupDirectory.Exists)
            try
            {
                backupDirectory.Create();
                backupDirectory.Refresh();
            }
            catch (Exception e)
            {
                Log.ForContext("backupFile", fileToBackup.SafeObjectDump())
                    .ForContext("backupDirectory", backupDirectory.SafeObjectDump())
                    .Error(e, "Error Creating Backup Directory");
                progress?.Report($"Problem creating backup directory! {e.Message}");
                return false;
            }

        var backupFile = new FileInfo(Path.Combine(backupDirectory.FullName,
            $"{Path.GetFileNameWithoutExtension(fileToBackup.Name)}-PreGeoTag{DateTime.Now:yyyyMMdd-HHmm}{fileToBackup.Extension}"));

        var counter = 0;
        while (backupFile.Exists && counter < 10000)
            backupFile = new FileInfo(Path.Combine(backupDirectory.FullName,
                $"{Path.GetFileNameWithoutExtension(fileToBackup.Name)}-PreGeoTag{DateTime.Now:yyyyMMdd-HHmm}-{++counter:0000}{fileToBackup.Extension}"));

        if (backupFile.Exists)
        {
            Log.ForContext("backupFileLastIteration", fileToBackup.SafeObjectDump())
                .ForContext("backupDirectory", backupDirectory.SafeObjectDump())
                .Error("Error Creating a Unique Backup File Name");
            progress?.Report(
                $"Could Not find a Unique File Name to backup {fileToBackup.FullName} in {backupDirectory.FullName}?");
            return false;
        }

        try
        {
            fileToBackup.CopyTo(backupFile.FullName);
        }
        catch (Exception e)
        {
            Log.ForContext("backupFile", fileToBackup.SafeObjectDump())
                .ForContext("backupDirectory", backupDirectory.SafeObjectDump()).Error(e, "Error Copying Backup File");
            progress?.Report($"Problem creating backup file! {e.Message}");
            return false;
        }

        return true;
    }
}