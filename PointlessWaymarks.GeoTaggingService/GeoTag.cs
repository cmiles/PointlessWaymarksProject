using System.Text;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LoggingTools;
using PointlessWaymarks.SpatialTools;
using Serilog;
using File = TagLib.File;

namespace PointlessWaymarks.GeoTaggingService;

public class GeoTag
{
    public async Task<GeoTagResult> Tag(List<FileInfo> filesToTag, List<IGpxService> gpxServices, bool testRun,
        bool createBackupBeforeWritingMetadata, int pointMustBeWithinMinutes, int adjustCreatedTimeInMinutes,
        bool overwriteExistingLatLong, string? exifToolFullName = null, IProgress<string>? progress = null)
    {
        var baseRunInformation =
            $"GeoTag - Starting {DateTime.Now} - {filesToTag.Count} Files, {gpxServices.Count} GpxServices, Create Backup {createBackupBeforeWritingMetadata}, {pointMustBeWithinMinutes} Within Minutes, {adjustCreatedTimeInMinutes} Adjustment, Overwrite {overwriteExistingLatLong}";

        Log.Information(baseRunInformation);
        var returnTitle = baseRunInformation;
        var returnNotes = new StringBuilder();
        var returnFileResults = new List<GeoTagFileResult>();


        if (!createBackupBeforeWritingMetadata && !testRun)
        {
            var backupWarning =
                "WARNING - writing metadata into files is never going to be 100% without errors - this method is running without creating backups ('in place updates only') - in the future consider allowing backups to be created to avoid any unfortunate incidents where metadata additions result in corrupted files!";
            progress?.Report(backupWarning);
            returnNotes.AppendLine(backupWarning);
            returnNotes.AppendLine();
        }

        if (!filesToTag.Any())
        {
            var noFilesMessage = "GeoTag - No files to tag, ending...";
            progress?.Report(noFilesMessage);
            returnNotes.Append(noFilesMessage);
            return new GeoTagResult(returnTitle, returnNotes.ToString(), returnFileResults);
        }

        if (!gpxServices.Any())
        {
            var noGpxServicesMessage = "GeoTag - No GPX Services to tag with, ending...";
            progress?.Report(noGpxServicesMessage);
            returnNotes.AppendLine(noGpxServicesMessage);
            return new GeoTagResult(returnTitle, returnNotes.ToString(), returnFileResults);
        }

        var exifTool = FileMetadataTools.ExifToolExecutable(exifToolFullName);

        var supportedFileExtensions = exifTool.isPresent
            ? FileMetadataTools.TagSharpSupportedExtensions.Union(FileMetadataTools.ExifToolWriteSupportedExtensions)
                .ToList()
            : FileMetadataTools.TagSharpSupportedExtensions;

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
                $"GeoTag - Found {notSupportedFiles.Count} with extensions that are not in this program's of supported extensions {(exifTool.isPresent ? "" : "(ExifTool was not found and expands considerably the supported file extensions) ")}.");

            progress?.Report(
                $"GeoTag - {notSupportedFileExtensions.Length} Extensions without support: {notSupportedFileExtensions}");

            progress?.Report(
                $"GeoTag - {notSupportedFiles.Count} Files without support: {string.Join(", ", notSupportedFiles.Select(x => x.FullName).OrderBy(x => x))}");

            returnNotes.AppendLine(
                exifTool.isPresent
                    ? "There are files that are not supported by this program - this program uses TagSharp and ExifTool - generally if ExifTool doesn't report read/write capabilities for a file extension it is not supported."
                    : "There are files that are not supported by this program - this program can support additional file formats if you download and setup ExifTool - https://oliverbetz.de/pages/Artikel/ExifTool-for-Windows.");

            notSupportedFiles.ForEach(x => returnFileResults.Add(new GeoTagFileResult(x.FullName, "Not Supported",
                string.Empty,
                $"The file extension {x.Extension} is not supported - file not processed.")));
        }

        progress?.Report($"GeoTag - {supportedFiles.Count} Supported Files");

        var frozenExecutionTime = DateTime.Now;

        var listOfUtcAndFileToProcess = new List<(DateTime createdUtc, FileInfo file)>();

        progress?.Report("GeoTag - Getting photo UTC Time (required) and checking for existing Lat/Long");
        var counter = 0;

        foreach (var loopFile in supportedFiles.OrderBy(x => x.FullName).ToList())
        {
            if (++counter % 50 == 0)
                progress?.Report($"GeoTag - Processing Metadata - File {counter} of {supportedFiles.Count}");

            if (!loopFile.Exists)
            {
                progress?.Report($"GeoTag - File {loopFile.FullName} doesn't exist - skipping");
                returnFileResults.Add(new GeoTagFileResult(loopFile.FullName, "File Not Found", string.Empty,
                    "The file was not found - file not processed."));
                continue;
            }

            var hasExistingLatLong = await FileMetadataTools.FileHasLatLong(loopFile, progress);

            if (!overwriteExistingLatLong && hasExistingLatLong)
            {
                progress?.Report(
                    $"GeoTag - File {loopFile.FullName} Already Has a GeoLocation and Overwrite Existing is Set to {overwriteExistingLatLong} - skipping");
                var metadataLocation = await FileMetadataTools.Location(loopFile, false, progress);

                returnFileResults.Add(new GeoTagFileResult(loopFile.FullName, "Skipped",
                    "File has Geolocation Metadata and this run is not set to Overwrite existing data.", string.Empty,
                    null,
                    metadataLocation.Latitude, metadataLocation.Longitude, metadataLocation.Elevation));
                continue;
            }

            var createdOnUtc = await FileMetadataTools.FileUtcCreatedOn(loopFile, progress);

            if (createdOnUtc == null)
            {
                progress?.Report(
                    $"GeoTag - Valid TagTimeZone not found in ExifSubIfdDirectory for {loopFile.FullName} - skipping");

                var metadataLocation = await FileMetadataTools.Location(loopFile, false, progress);

                returnFileResults.Add(new GeoTagFileResult(loopFile.FullName, "Skipped",
                    "No Valid TagTimeZone found - this value is needed to determine the UTC time of a photo.",
                    string.Empty, null, metadataLocation.Latitude, metadataLocation.Longitude,
                    metadataLocation.Elevation));
                continue;
            }

            listOfUtcAndFileToProcess.Add((createdOnUtc.Value.AddMinutes(adjustCreatedTimeInMinutes), loopFile));
        }

        progress?.Report(
            $"Found {listOfUtcAndFileToProcess.Count} files to Process out of {supportedFiles.Count} supported Files");

        var pointLists = new List<(DateTime start, DateTime end, List<WaypointAndSource> waypoints)>();

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

            var pointsCollection = new List<WaypointAndSource>();

            foreach (var loopService in gpxServices)
                pointsCollection.AddRange(
                    (await loopService.GetGpxPoints(loopUtc.createdUtc, progress)).Where(x =>
                        x.Waypoint.TimestampUtc != null));

            if (!pointsCollection.Any()) continue;

            var toAdd = (pointsCollection.MinBy(x => x.Waypoint.TimestampUtc!.Value).Waypoint.TimestampUtc.Value,
                pointsCollection.MaxBy(x => x.Waypoint.TimestampUtc!.Value).Waypoint.TimestampUtc.Value,
                pointsCollection);

            progress?.Report(
                $"GeoTag - For {loopUtc.file} added {toAdd.pointsCollection.Count} points from UTC {toAdd.Item1} to {toAdd.Item2}");

            pointLists.Add(toAdd);
        }

        var allPoints = pointLists.SelectMany(x => x.waypoints).ToList();

        progress?.Report(
            $"Found a total of {allPoints.Count} points to check for files within {adjustCreatedTimeInMinutes} minutes of for location tagging");

        counter = 0;

        returnNotes.AppendLine($"GeoTag Processing - {listOfUtcAndFileToProcess.Count} Files:");

        foreach (var loopFile in listOfUtcAndFileToProcess.OrderBy(x => x.file.FullName).ToList())
        {
            if (++counter % 50 == 0)
                progress?.Report(
                    $"GeoTag - Finding Location and Writing Metadata - File {counter} of {listOfUtcAndFileToProcess.Count}");

            var possibleTagPoints = allPoints.Where(x =>
                loopFile.createdUtc.AddMinutes(-Math.Abs(pointMustBeWithinMinutes)) >= x.Waypoint.TimestampUtc &&
                x.Waypoint.TimestampUtc <= loopFile.createdUtc.AddMinutes(Math.Abs(pointMustBeWithinMinutes))).ToList();

            if (!possibleTagPoints.Any())
            {
                progress?.Report(
                    $"GeoTag - No Matching Points Found for {loopFile.file.FullName}");

                var metadataLocation = await FileMetadataTools.Location(loopFile.file, false, progress);

                returnFileResults.Add(new GeoTagFileResult(loopFile.file.FullName, "No Matching GPX Data",
                    $"No Matching Points Found within {pointMustBeWithinMinutes} Minutes.", string.Empty,
                    loopFile.createdUtc,
                    metadataLocation.Latitude, metadataLocation.Longitude, metadataLocation.Elevation));
                continue;
            }

            var closest = possibleTagPoints.MinBy(x =>
                Math.Abs(loopFile.createdUtc.Subtract(x.Waypoint.TimestampUtc.Value).TotalMicroseconds));

            var latitude = closest.Waypoint.Latitude.Value;
            var longitude = closest.Waypoint.Longitude.Value;
            double? elevation = null;
            if (closest.Waypoint.ElevationInMeters is not null) elevation = closest.Waypoint.ElevationInMeters;
            else
                try
                {
                    elevation = await ElevationService.OpenTopoNedElevation(closest.Waypoint.Latitude,
                        closest.Waypoint.Longitude,
                        progress);
                }
                catch (Exception e)
                {
                    Log.Verbose($"GeoTag - Failed to get Elevation, Silent Error - {e.Message}");
                }

            progress?.Report(
                $"GeoTag - For {loopFile.file.FullName} found Lat: {latitude} Long: {longitude} Elevation {elevation} from {closest.Source}");


            if (createBackupBeforeWritingMetadata && !testRun)
            {
                var backUpSuccessful = WriteFileToBackupDirectory(frozenExecutionTime, loopFile.file, progress);
                if (!backUpSuccessful)
                {
                    returnFileResults.Add(new GeoTagFileResult(loopFile.file.FullName, "Backup Error",
                        "Backup File could not be written - no attempt to write Geolocation made.", closest.Source,
                        loopFile.createdUtc,
                        latitude, longitude, elevation));
                    progress?.Report(
                        $"GeoTag - Skipping {loopFile.file.FullName} - Found GeoTag information but could not create a backup - skipping this file.");
                }
            }

            if (FileMetadataTools.TagSharpSupportedExtensions.Contains(loopFile.file.Extension))
            {
                if (testRun)
                {
                    returnFileResults.Add(new GeoTagFileResult(loopFile.file.FullName, "Test Success",
                        "This is a Test Run - Would Write {latitude}, {longitude} - Elevation: {elevation} with TagSharp.",
                        closest.Source, loopFile.createdUtc, latitude, longitude, elevation));
                    progress?.Report(
                        $"GeoTag - TestRun - would result in a TagSharp write of {latitude}, {longitude} - Elevation: {elevation} to {loopFile.file.FullName}");
                    continue;
                }

                if (File.Create(loopFile.file.FullName) is TagLib.Image.File tagSharpFile)
                    try
                    {
                        tagSharpFile.ImageTag.Latitude = latitude;
                        tagSharpFile.ImageTag.Longitude = longitude;
                        if (elevation is not null) tagSharpFile.ImageTag.Altitude = elevation;
                        tagSharpFile.Save();
                        returnFileResults.Add(new GeoTagFileResult(loopFile.file.FullName, "Success",
                            $"Wrote {latitude}, {longitude} - Elevation: {elevation} with TagSharp", closest.Source,
                            loopFile.createdUtc,
                            latitude, longitude, elevation));
                        progress?.Report("GeoTag - Wrote Metadata with TagSharp");
                        continue;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e,
                            $"Error Tagging {loopFile.file.FullName} with TagSharp - {latitude}, {longitude} - Elevation: {elevation}");
                        returnFileResults.Add(new GeoTagFileResult(loopFile.file.FullName, "Error",
                            $"Trying to write {latitude}, {longitude} - Elevation: {elevation} with TagSharp resulted in an error - {e.Message}",
                            closest.Source, loopFile.createdUtc, latitude, longitude, elevation));
                        continue;
                    }
            }

            var exifToolParameters = elevation is null
                ? $"-GPSLatitude*={latitude} -GPSLongitude*={longitude} -overwrite_original \"{loopFile.file.FullName}\" "
                : $"-GPSLatitude*={latitude} -GPSLongitude*={longitude} -GPSAltitude*={elevation} -overwrite_original \"{loopFile.file.FullName}\"";
            try
            {
                if (testRun)
                {
                    returnFileResults.Add(new GeoTagFileResult(loopFile.file.FullName, "Test Success",
                        $"This is a Test Run - Would Write {latitude}, {longitude} - Elevation: {elevation} with ExifTool.",
                        closest.Source, loopFile.createdUtc, latitude, longitude, elevation));
                    continue;
                }

                var exifToolWriteOutcome =
                    ProcessRunner.Execute(exifTool.exifToolFile.FullName, exifToolParameters, progress);

                if (!exifToolWriteOutcome.success)
                {
                    Log.ForContext("standardOutput", exifToolWriteOutcome.standardOutput)
                        .ForContext("errorOutput", exifToolWriteOutcome.errorOutput)
                        .ForContext("success", exifToolWriteOutcome.success)
                        .ForContext("exifToolParameters", exifToolParameters)
                        .ForContext("exifTool", exifTool.SafeObjectDump())
                        .Error($"Writing with ExifTool did not Succeed - {loopFile.file.FullName}");

                    returnFileResults.Add(new GeoTagFileResult(loopFile.file.FullName, "ExifTool Failure",
                        $"Trying to write {latitude}, {longitude} - Elevation: {elevation} with TagSharp resulted in an error.",
                        closest.Source, loopFile.createdUtc, latitude, longitude, elevation));

                    continue;
                }
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"Error Tagging {loopFile.file.FullName} with ExifTool - {latitude}, {longitude} - Elevation: {elevation}");
                returnFileResults.Add(new GeoTagFileResult(loopFile.file.FullName, "Error",
                    $"Trying to write {latitude}, {longitude} - Elevation: {elevation} with ExifTool resulted in an error - {e.Message}",
                    closest.Source, loopFile.createdUtc, latitude, longitude, elevation));
                continue;
            }

            returnFileResults.Add(new GeoTagFileResult(loopFile.file.FullName, "Success",
                $"Wrote {latitude}, {longitude} - Elevation: {elevation} with ExifTool", closest.Source,
                loopFile.createdUtc, latitude,
                longitude, elevation));

            progress?.Report(
                $"GeoTag - Wrote Metadata with ExifTool - {exifTool.exifToolFile.FullName} {exifToolParameters}");
        }

        return new GeoTagResult(returnTitle, returnNotes.ToString(), returnFileResults);
    }

    public static bool WriteFileToBackupDirectory(DateTime executionTime, FileInfo fileToBackup,
        IProgress<string>? progress)
    {
        var directoryInfo = new DirectoryInfo(fileToBackup.DirectoryName ?? string.Empty);

        var backupDirectory = UniqueFileTools.UniqueDirectory(Path.Combine(directoryInfo.FullName,
            $"PwGeoTagBackup-{executionTime:yyyy-MM-dd-HHmmss}"));

        var backupFile = UniqueFileTools.UniqueFile(backupDirectory, fileToBackup.Name);

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

    public record GeoTagFileResult(string FileName, string Result, string Notes, string Source,
        DateTime? UtcDateTime = null, double? Latitude = null,
        double? Longitude = null, double? Elevation = null);

    public record GeoTagResult(string Title, string Notes, List<GeoTagFileResult> FileResults);
}