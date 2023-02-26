using System.Text;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;
using Serilog;
using File = TagLib.File;

namespace PointlessWaymarks.GeoTaggingService;

public class GeoTag
{
    public async Task<GeoTagProduceActionsResult> ProduceGeoTagActions(List<FileInfo> filesToTag,
        List<IGpxService> gpxServices, int pointMustBeWithinMinutes,
        int adjustCreatedTimeInMinutes,
        bool overwriteExistingLatLong, string? exifToolFullName = null, IProgress<string>? progress = null)
    {
        var baseRunInformation =
            $"GeoTag - Produce File Actions - Starting {DateTime.Now} - {filesToTag.Count} Files, {gpxServices.Count} GpxServices, {pointMustBeWithinMinutes} Within Minutes, {adjustCreatedTimeInMinutes} Adjustment, Overwrite {overwriteExistingLatLong}, ExifTool {exifToolFullName}";

        Log.Information(baseRunInformation);
        var returnTitle = baseRunInformation;
        var returnNotes = new StringBuilder();
        var returnFileResults = new List<GeoTagFileAction>();

        if (!filesToTag.Any())
        {
            var noFilesMessage = "GeoTag - No files to tag, ending...";
            progress?.Report(noFilesMessage);
            returnNotes.Append(noFilesMessage);
            return new GeoTagProduceActionsResult(returnTitle, returnNotes.ToString(), returnFileResults);
        }

        if (!gpxServices.Any())
        {
            var noGpxServicesMessage = "GeoTag - No GPX Services to tag with, ending...";
            progress?.Report(noGpxServicesMessage);
            returnNotes.AppendLine(noGpxServicesMessage);
            return new GeoTagProduceActionsResult(returnTitle, returnNotes.ToString(), returnFileResults);
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
                $"GeoTag - Found {notSupportedFiles.Count} with extensions that are not supported");

            progress?.Report(
                $"GeoTag - {notSupportedFileExtensions.Length} Extensions without support: {notSupportedFileExtensions}");

            progress?.Report(
                $"GeoTag - {notSupportedFiles.Count} Files without support: {string.Join(", ", notSupportedFiles.Select(x => x.FullName).OrderBy(x => x))}");

            notSupportedFiles.ForEach(x => returnFileResults.Add(new GeoTagFileAction(x.FullName, false,
                $"The file extension {x.Extension} is not supported - file not processed.", string.Empty)));
        }

        progress?.Report($"GeoTag - {supportedFiles.Count} Supported Files");

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
                returnFileResults.Add(new GeoTagFileAction(loopFile.FullName, false,
                    "The file was not found - file not processed.", string.Empty));
                continue;
            }

            var hasExistingLatLong = await FileMetadataTools.FileHasLatLong(loopFile, progress);

            if (!overwriteExistingLatLong && hasExistingLatLong)
            {
                progress?.Report(
                    $"GeoTag - File {loopFile.FullName} Already Has a GeoLocation and Overwrite Existing is Set to {overwriteExistingLatLong} - skipping");
                var metadataLocation = await FileMetadataTools.Location(loopFile, false, progress);

                returnFileResults.Add(new GeoTagFileAction(loopFile.FullName, false,
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

                returnFileResults.Add(new GeoTagFileAction(loopFile.FullName, false,
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


            var timestampMin = pointsCollection.Where(x => x.Waypoint.TimestampUtc != null).MinBy(x => x.Waypoint.TimestampUtc!.Value)!
                .Waypoint.TimestampUtc;
            var timestampMax = pointsCollection.Where(x => x.Waypoint.TimestampUtc != null).MaxBy(x => x.Waypoint.TimestampUtc!.Value)!
                .Waypoint.TimestampUtc;

            if (timestampMin != null && timestampMax != null)
            {
                var toAdd = (timestampMin.Value,
                    timestampMax.Value, pointsCollection);

                pointLists.Add(toAdd);

                progress?.Report(
                    $"GeoTag - For {loopUtc.file} added {toAdd.pointsCollection.Count} points from UTC {toAdd.Item1} to {toAdd.Item2}");
            }
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
                x.Waypoint.TimestampUtc >= loopFile.createdUtc.AddMinutes(-Math.Abs(pointMustBeWithinMinutes)) &&
                x.Waypoint.TimestampUtc <= loopFile.createdUtc.AddMinutes(Math.Abs(pointMustBeWithinMinutes))).ToList();

            if (!possibleTagPoints.Any())
            {
                progress?.Report(
                    $"GeoTag - No Matching Points Found for {loopFile.file.FullName}");

                var metadataLocation = await FileMetadataTools.Location(loopFile.file, false, progress);

                returnFileResults.Add(new GeoTagFileAction(loopFile.file.FullName, false,
                    $"No Matching Points Found within {pointMustBeWithinMinutes} Minutes.", string.Empty,
                    loopFile.createdUtc,
                    metadataLocation.Latitude, metadataLocation.Longitude, metadataLocation.Elevation));
                continue;
            }

            var closest = possibleTagPoints.Where(x => x.Waypoint.TimestampUtc != null).MinBy(x =>  Math.Abs(loopFile.createdUtc.Subtract(x.Waypoint.TimestampUtc!.Value).TotalMicroseconds));

            if (closest == null) continue;

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

            returnFileResults.Add(new GeoTagFileAction(loopFile.file.FullName, true,
                $"Found {latitude}, {longitude} - Elevation: {elevation}.",
                closest.Source, loopFile.createdUtc, latitude, longitude, elevation));
        }

        return new GeoTagProduceActionsResult(returnTitle, returnNotes.ToString(), returnFileResults);
    }

    public Task<GeoTagWriteMetadataToFilesResult> WriteGeoTagActions(List<GeoTagFileAction> filesToTag,
        bool createBackupBeforeWritingMetadata, bool backupIntoDefaultStorage, string? exifToolFullName = null,
        IProgress<string>? progress = null)
    {
        var baseRunInformation =
            $"GeoTag - Write Metadata - Starting {DateTime.Now} - {filesToTag.Count} Files, Create Backup {createBackupBeforeWritingMetadata}";

        Log.Information(baseRunInformation);
        var returnTitle = baseRunInformation;
        var returnNotes = new StringBuilder();
        var returnFileResults = new List<GeoTagMetadataWrite>();

        if (!createBackupBeforeWritingMetadata)
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
            return Task.FromResult(new GeoTagWriteMetadataToFilesResult(returnTitle, returnNotes.ToString(), returnFileResults));
        }

        var exifTool = FileMetadataTools.ExifToolExecutable(exifToolFullName);

        var supportedFileExtensions = exifTool.isPresent
            ? FileMetadataTools.TagSharpSupportedExtensions.Union(FileMetadataTools.ExifToolWriteSupportedExtensions)
                .ToList()
            : FileMetadataTools.TagSharpSupportedExtensions;

        var frozenExecutionTime = DateTime.Now;

        foreach (var loopFile in filesToTag.OrderBy(x => x.FileName).ToList())
        {
            var fileToWriteTo = new FileInfo(loopFile.FileName);

            if (!fileToWriteTo.Exists)
            {
                returnFileResults.Add(new GeoTagMetadataWrite(loopFile.FileName, false, "File Does Not Exist",
                    loopFile.Source, loopFile.Latitude, loopFile.Longitude, loopFile.Elevation));
                continue;
            }

            if (!supportedFileExtensions.Contains(fileToWriteTo.Extension, StringComparer.OrdinalIgnoreCase))
            {
                returnFileResults.Add(new GeoTagMetadataWrite(loopFile.FileName, false, "File Type Not Supported",
                    loopFile.Source, loopFile.Latitude, loopFile.Longitude, loopFile.Elevation));
                continue;
            }

            if (createBackupBeforeWritingMetadata)
            {
                bool backUpSuccessful;

                if (backupIntoDefaultStorage)
                    backUpSuccessful = UniqueFileTools.WriteFileToDefaultStorageDirectoryBackupDirectory(
                        frozenExecutionTime, "PwGeoTag", fileToWriteTo,
                        progress);
                else
                    backUpSuccessful = UniqueFileTools.WriteFileToInPlaceBackupDirectory(frozenExecutionTime,
                        "PwGeoTag", fileToWriteTo,
                        progress);

                if (!backUpSuccessful)
                {
                    returnFileResults.Add(new GeoTagMetadataWrite(loopFile.FileName, false, "Backup Failed",
                        loopFile.Source, loopFile.Latitude, loopFile.Longitude, loopFile.Elevation));
                    progress?.Report(
                        $"GeoTag - Skipping {fileToWriteTo.FullName} - Found GeoTag information but could not create a backup - skipping this file.");
                    continue;
                }
            }

            if (FileMetadataTools.TagSharpSupportedExtensions.Contains(fileToWriteTo.Extension,
                    StringComparer.OrdinalIgnoreCase))
            {
                if (File.Create(fileToWriteTo.FullName) is TagLib.Image.File tagSharpFile)
                    try
                    {
                        tagSharpFile.ImageTag.Latitude = loopFile.Latitude;
                        tagSharpFile.ImageTag.Longitude = loopFile.Longitude;
                        if (loopFile.Elevation is not null) tagSharpFile.ImageTag.Altitude = loopFile.Elevation;
                        tagSharpFile.Save();
                        returnFileResults.Add(new GeoTagMetadataWrite(loopFile.FileName, true,
                            "Wrote to File with TagSharp", loopFile.Source, loopFile.Latitude, loopFile.Longitude, loopFile.Elevation));
                        progress?.Report("GeoTag - Wrote Metadata with TagSharp");
                        continue;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e,
                            $"Error Tagging {fileToWriteTo.FullName} with TagSharp - {loopFile.Latitude}, {loopFile.Longitude} - Elevation: {loopFile.Elevation}");
                        returnFileResults.Add(new GeoTagMetadataWrite(fileToWriteTo.FullName, false,
                            $"Trying to write {loopFile.Latitude}, {loopFile.Longitude} - Elevation: {loopFile.Elevation} with TagSharp resulted in an error - {e.Message}",
                            loopFile.Source, loopFile.Latitude, loopFile.Longitude, loopFile.Elevation));
                        continue;
                    }
            }

            var exifToolParameters = loopFile.Elevation is null
                ? $"-GPSLatitude*={loopFile.Latitude} -GPSLongitude*={loopFile.Longitude} -overwrite_original \"{fileToWriteTo.FullName}\" "
                : $"-GPSLatitude*={loopFile.Latitude} -GPSLongitude*={loopFile.Longitude} -GPSAltitude*={loopFile.Elevation} -overwrite_original \"{fileToWriteTo.FullName}\"";
            try
            {
                var exifToolWriteOutcome =
                    ProcessTools.Execute(exifTool.exifToolFile!.FullName, exifToolParameters, progress);

                if (!exifToolWriteOutcome.success)
                {
                    Log.ForContext("standardOutput", exifToolWriteOutcome.standardOutput)
                        .ForContext("errorOutput", exifToolWriteOutcome.errorOutput)
                        .ForContext("success", exifToolWriteOutcome.success)
                        .ForContext("exifToolParameters", exifToolParameters)
                        .ForContext("exifTool", exifTool.SafeObjectDump())
                        .Error($"Writing with ExifTool did not Succeed - {fileToWriteTo.FullName}");

                    returnFileResults.Add(new GeoTagMetadataWrite(fileToWriteTo.FullName, false,
                        $"Trying to write {loopFile.Latitude}, {loopFile.Longitude} - Elevation: {loopFile.Elevation} with TagSharp resulted in an error.",
                        loopFile.Source, loopFile.Latitude, loopFile.Longitude, loopFile.Elevation));

                    continue;
                }
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"Error Tagging {fileToWriteTo.FullName} with ExifTool - {loopFile.Latitude}, {loopFile.Longitude} - Elevation: {loopFile.Elevation}");
                returnFileResults.Add(new GeoTagMetadataWrite(fileToWriteTo.FullName, false,
                    $"Trying to write {loopFile.Latitude}, {loopFile.Longitude} - Elevation: {loopFile.Elevation} with ExifTool resulted in an error - {e.Message}",
                    loopFile.Source, loopFile.Latitude, loopFile.Longitude, loopFile.Elevation));
                continue;
            }

            returnFileResults.Add(new GeoTagMetadataWrite(fileToWriteTo.FullName, true,
                $"Wrote {loopFile.Latitude}, {loopFile.Longitude} - Elevation: {loopFile.Elevation} with ExifTool", loopFile.Source,
                loopFile.Latitude,
                loopFile.Longitude, loopFile.Elevation));

            progress?.Report(
                $"GeoTag - Wrote Metadata with ExifTool - {exifTool.exifToolFile.FullName} {exifToolParameters}");
        }

        return Task.FromResult(new GeoTagWriteMetadataToFilesResult(returnTitle, returnNotes.ToString(), returnFileResults));
    }


    public record GeoTagFileAction(string FileName, bool ShouldWriteMetadata, string Notes, string Source,
        DateTime? UtcDateTime = null, double? Latitude = null,
        double? Longitude = null, double? Elevation = null);

    public record GeoTagProduceActionsResult(string Title, string Notes, List<GeoTagFileAction> FileResults);

    public record GeoTagWriteMetadataToFilesResult(string Title, string Notes,
        List<GeoTagMetadataWrite> FileResults);

    public record GeoTagMetadataWrite(string FileName, bool WroteMetadata, string Notes, string Source, double? Latitude = null,
        double? Longitude = null, double? Elevation = null);
}