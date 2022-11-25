using System.Text.Json;
using System.Text.RegularExpressions;
using NetTopologySuite.Features;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.SpatialTools;
using Serilog;

namespace PointlessWaymarks.FeatureIntersectionTags;

public static class Intersection
{
    public static async Task<List<IntersectFileTaggingResult>> FileIntersectionTags(this List<FileInfo> toTag,
        IntersectSettings settings
        , CancellationToken cancellationToken,
        IProgress<string>? progress = null)
    {
        var sourceFileAndFeatures = new List<IntersectFileTaggingResult>();
        toTag.ForEach(x => sourceFileAndFeatures.Add(new IntersectFileTaggingResult(x)));

        var metadataFiles = sourceFileAndFeatures.Where(x =>
                FileMetadataTools.ExifToolWriteSupportedExtensions.Any(y =>
                    y.Equals(x.FileToTag.Extension, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var loopFile in metadataFiles)
        {
            var location = await FileMetadataTools.Location(loopFile.FileToTag, false, progress);

            if (!location.HasValidLocation()) continue;

            var feature = new Feature(PointTools.Wgs84Point(location.Longitude!.Value, location.Latitude!.Value),
                new AttributesTable());

            loopFile.IntersectInformation = new IntersectResult(feature);
        }

        var gpxFiles = sourceFileAndFeatures.Where(x =>
            x.FileToTag.Extension.Equals(".GPX")).ToList();

        foreach (var loopGpx in gpxFiles)
        {
            var trackLines = await GpxTools.TrackLinesFromGpxFile(loopGpx.FileToTag);
            var routeLines = await GpxTools.RouteLinesFromGpxFile(loopGpx.FileToTag);
            var waypointPoints = await GpxTools.WaypointPointsFromGpxFile(loopGpx.FileToTag);

            loopGpx.IntersectInformation = new IntersectResult(trackLines.features.Cast<IFeature>().Union(routeLines.features)
                .Union(waypointPoints.features).ToList());
        }

        var geojsonFiles = sourceFileAndFeatures.Where(x =>
            x.FileToTag.Extension.Equals(".GEOJSON")).ToList();

        foreach (var loopGeojson in geojsonFiles)
        {
            var features = GeoJsonTools.DeserializeFileToFeatureCollection(loopGeojson.FileToTag.FullName).ToList();

            foreach (var loopFeature in features) loopFeature.Attributes.Add("title", loopGeojson.FileToTag.Name);

            loopGeojson.IntersectInformation = new IntersectResult(features);
        }

        sourceFileAndFeatures.Where(x => x.IntersectInformation != null).Select(x => x.IntersectInformation!).ToList()
            .IntersectionTags(settings,
                cancellationToken,
                progress);

        sourceFileAndFeatures.Where(x => x.IntersectInformation == null).ToList().ForEach(x =>
        {
            x.Result = "Not Supported";
            x.Notes = "File Type not supported";
        });

        return sourceFileAndFeatures;
    }

    public static List<IntersectResult> IntersectionTags(this List<IntersectResult> toCheck,
        IntersectSettings settings,
        CancellationToken cancellationToken, IProgress<string>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (settings.IntersectFiles.Any())
            toCheck.ProcessFileIntersections(settings.IntersectFiles.ToList(),
                cancellationToken, progress);

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(settings.PadUsDirectory) && settings.PadUsAttributesForTags.Any())
            toCheck.ProcessPadUsIntersections(settings.PadUsAttributesForTags.ToList(),
                settings.PadUsDirectory, cancellationToken,
                progress);

        return toCheck;
    }

    /// <summary>
    ///     Checks the submitted List of IFeatures for tags based on the submitted settings file - if the settings
    ///     file is blank of invalid an empty list is returned.
    /// </summary>
    /// <param name="intersectSettingsFile"></param>
    /// <param name="toCheck"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static List<IntersectResult> IntersectionTags(this List<IntersectResult> toCheck,
        string intersectSettingsFile,
        CancellationToken cancellationToken, IProgress<string>? progress = null)
    {
        if (string.IsNullOrEmpty(intersectSettingsFile))
        {
            progress?.Report("No Settings File Submitted - returning nothing...");

            return toCheck;
        }

        if (!toCheck.Any())
        {
            progress?.Report("No Features to Check - returning nothing...");

            return toCheck;
        }

        progress?.Report($"Getting Settings from {intersectSettingsFile}");
        var settings = JsonSerializer.Deserialize<IntersectSettings>(File.ReadAllText(intersectSettingsFile));

        if (settings == null)
        {
            progress?.Report($"The settings file {intersectSettingsFile} did not deserialized to valid settings...");

            return toCheck;
        }

        return toCheck.IntersectionTags(settings, cancellationToken, progress);
    }

    public static List<string> IntersectionTags(this IFeature toCheck, string intersectSettingsFile,
        CancellationToken cancellationToken, IProgress<string>? progress = null)
    {
        if (string.IsNullOrEmpty(intersectSettingsFile))
        {
            progress?.Report("No Settings File Submitted - returning nothing...");

            return new List<string>();
        }

        var intersectionResult = new IntersectResult(toCheck);

        progress?.Report($"Getting Settings from {intersectSettingsFile}");
        var settings = JsonSerializer.Deserialize<IntersectSettings>(File.ReadAllText(intersectSettingsFile));

        if (settings == null)
        {
            progress?.Report($"The settings file {intersectSettingsFile} did not deserialized to valid settings...");

            return new List<string>();
        }

        return intersectionResult.AsList().IntersectionTags(settings, cancellationToken, progress)
            .SelectMany(x => x.Tags).ToList();
    }

    public static IntersectResult IntersectionTags(this IntersectResult toCheck, string intersectSettingsFile,
        CancellationToken cancellationToken, IProgress<string>? progress = null)
    {
        if (string.IsNullOrEmpty(intersectSettingsFile))
        {
            progress?.Report("No Settings File Submitted - returning nothing...");

            return toCheck;
        }

        progress?.Report($"Getting Settings from {intersectSettingsFile}");
        var settings = JsonSerializer.Deserialize<IntersectSettings>(File.ReadAllText(intersectSettingsFile));

        if (settings == null)
        {
            progress?.Report($"The settings file {intersectSettingsFile} did not deserialized to valid settings...");

            return toCheck;
        }

        return toCheck.AsList().IntersectionTags(settings, cancellationToken, progress).Single();
    }

    public static List<IntersectResult> ProcessFileIntersections(this List<IntersectResult> toCheck,
        List<FeatureFile> intersectFiles,
        CancellationToken cancellationToken,
        IProgress<string>? progress = null)
    {
        var counter = 0;

        foreach (var loopIntersectFile in intersectFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            counter++;

            progress?.Report(
                $"Processing Feature Intersect - {loopIntersectFile.Name}, {loopIntersectFile.FileName} - {counter} of {intersectFiles.Count}");

            var intersectFileInfo = new FileInfo(loopIntersectFile.FileName);

            if (!intersectFileInfo.Exists)
            {
                progress?.Report($"  Skipping file {loopIntersectFile.FileName} - Does Not Exist.");
                continue;
            }

            var intersectFeatures = GeoJsonTools.DeserializeFileToFeatureCollection(loopIntersectFile.FileName);

            var referenceFeatureCounter = 0;
            progress?.Report(
                $" Processing {intersectFeatures.Count} Reference Features against {toCheck.Count} Submitted Feature Sets");

            foreach (var loopIntersectFeature in intersectFeatures)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (++referenceFeatureCounter % 1000 == 0)
                    progress?.Report(
                        $" Processing {loopIntersectFile.Name} - Feature {referenceFeatureCounter} of {intersectFeatures.Count}");

                foreach (var loopCheck in toCheck)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (loopCheck.Features.Any(x => x.Geometry.Intersects(loopIntersectFeature.Geometry)))
                    {
                        foreach (var loopAttribute in loopIntersectFile.AttributesForTags)
                            if (loopIntersectFeature.Attributes.GetNames().Any(a => a == loopAttribute))
                            {
                                loopCheck.IntersectsWith.Add(loopIntersectFeature);
                                if (!loopCheck.Sources.Any(x =>
                                        loopIntersectFile.Name.Equals(x, StringComparison.OrdinalIgnoreCase)))
                                    loopCheck.Sources.Add(loopIntersectFile.Name);

                                var tagValue = (loopIntersectFeature.Attributes[loopAttribute]?.ToString() ??
                                                string.Empty).Trim();
                                if (!loopCheck.Tags.Any(x => x.Equals(tagValue, StringComparison.OrdinalIgnoreCase)))
                                    loopCheck.Tags.Add(tagValue);
                            }

                        if (!string.IsNullOrWhiteSpace(loopIntersectFile.TagAll) && !loopCheck.Tags.Any(x =>
                                x.Equals(loopIntersectFile.TagAll, StringComparison.OrdinalIgnoreCase)))
                        {
                            loopCheck.IntersectsWith.Add(loopIntersectFeature);
                            if (!loopCheck.Sources.Any(x =>
                                    loopIntersectFile.Name.Equals(x, StringComparison.OrdinalIgnoreCase)))
                                loopCheck.Sources.Add(loopIntersectFile.Name);

                            loopCheck.Tags.Add(loopIntersectFile.TagAll);
                        }
                    }
                }
            }
        }

        progress?.Report("Returning Features and Tags");

        return toCheck;
    }

    public static List<IntersectResult> ProcessPadUsIntersections(this List<IntersectResult> toCheck,
        List<string> attributesForTags,
        string padUsDirectory, CancellationToken cancellationToken,
        IProgress<string>? progress = null)
    {
        //Check for a valid setup - this requires some searching/checking to make sure the submitted
        //directory contains files that seem to make the by convention/documented patterns.
        if (string.IsNullOrWhiteSpace(padUsDirectory)) return toCheck;

        var padUsDirectoryInfo = new DirectoryInfo(padUsDirectory);

        if (!padUsDirectoryInfo.Exists)
        {
            progress?.Report($"PAD-US directory {padUsDirectory} doesn't exist...");
            return toCheck;
        }

        var geoJsonFiles = padUsDirectoryInfo.EnumerateFiles("*.geojson", SearchOption.TopDirectoryOnly).ToList();

        var regionsFile = geoJsonFiles
            .Where(x => x.Name.EndsWith("Regions.geojson", StringComparison.OrdinalIgnoreCase)).ToList();

        if (regionsFile.Count != 1)
        {
            progress?.Report(
                $"Couldn't find a single Region file matching *Regions.geojson in {padUsDirectoryInfo.FullName}");
            return toCheck;
        }

        var regionFiles = geoJsonFiles.Where(x =>
                Regex.IsMatch(x.Name, ".*[0-9]{1,2}.geojson", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
            .ToList();

        if (!regionsFile.Any())
        {
            progress?.Report(
                $"Couldn't find data for and of the DOI Regions matching *##.geojson in {padUsDirectoryInfo.FullName}");
            return toCheck;
        }

        //Get the region file information and create a list of Region and IntersectResults (this is
        //to allow just checking the IntersectResults against intersecting Regions)

        var doiRegionsFile = regionsFile.First();

        var doiRegionFeatures = GeoJsonTools.DeserializeFileToFeatureCollection(doiRegionsFile.FullName);

        var regionIntersections = new List<(string? region, IntersectResult feature)>();

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var loopCheck in toCheck)
        foreach (var loopDoiRegion in doiRegionFeatures)
            if (loopCheck.Features.Any(x => x.Geometry.Intersects(loopDoiRegion.Geometry)))
                regionIntersections.Add((loopDoiRegion.Attributes["REG_NUM"]?.ToString(), loopCheck));


        //Group by region and loop thru each region

        var counter = 0;

        var regionIntersectionsGroupedByRegion = regionIntersections.GroupBy(x => x.region).ToList();

        foreach (var loopDoiRegionGroup in regionIntersectionsGroupedByRegion)
        {
            cancellationToken.ThrowIfCancellationRequested();

            counter++;

            //Get the Region File and extract the Features
            var regionFile = regionFiles.FirstOrDefault(x =>
                x.Name.EndsWith($"{loopDoiRegionGroup.Key}.geojson", StringComparison.OrdinalIgnoreCase));

            if (regionFile == null)
            {
                progress?.Report($"A region file for Region {loopDoiRegionGroup.Key} was not found");
                continue;
            }

            progress?.Report(
                $"Processing PAD-US DOI Region File - {regionFile.Name} - {counter} of {regionIntersectionsGroupedByRegion.Count}");

            var regionFeatures = GeoJsonTools.DeserializeFileToFeatureCollection(regionFile.FullName).ToList();

            var referenceFeatureCounter = 0;
            progress?.Report(
                $" Processing {regionFeatures.Count} Reference Features against {toCheck.Count} Submitted Features");

            //Pull together all the IntersectResults Objects to loop thru
            var regionResults = loopDoiRegionGroup.Select(x => x.feature).ToList();

            //Outer loop is the Region's features, inner loop are the features to check for Intersection
            foreach (var loopRegionFeature in regionFeatures)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (++referenceFeatureCounter % 5000 == 0)
                    progress?.Report(
                        $" Processing {regionFile.Name} - Feature {referenceFeatureCounter} of {regionFeatures.Count}");

                foreach (var loopCheck in regionResults)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (loopCheck.Features.Any(x => x.Geometry.Intersects(loopRegionFeature.Geometry)))
                        foreach (var loopAttribute in attributesForTags)
                            if (loopRegionFeature.Attributes.GetNames().Any(a => a == loopAttribute))
                            {
                                loopCheck.IntersectsWith.Add(loopRegionFeature);
                                if (!loopCheck.Sources.Any(x =>
                                        regionFile.Name.Equals(x, StringComparison.OrdinalIgnoreCase)))
                                    loopCheck.Sources.Add(regionFile.Name);

                                var tagValue = (loopRegionFeature.Attributes[loopAttribute]?.ToString() ??
                                                string.Empty).Trim();
                                if (!loopCheck.Tags.Any(x =>
                                        x.Equals(tagValue, StringComparison.OrdinalIgnoreCase)))
                                    loopCheck.Tags.Add(tagValue);
                            }
                }
            }

            progress?.Report("Returning PAD-US Features and Tags");
        }

        return toCheck;
    }

    public static async Task<List<IntersectFileTaggingResult>> WriteTagsToFiles(
        this List<IntersectFileTaggingResult> toWrite, bool testRun,
        bool createBackupBeforeWritingMetadata, bool tagsToLower, bool sanitizeTags, string? exifToolFullName,
        CancellationToken cancellationToken, int tagMaxCharacterLength = 256,
        IProgress<string>? progress = null)
    {
        //Exit if nothing to process
        if (!toWrite.Any(x => x.IntersectInformation != null && x.IntersectInformation.Tags.Any())) return toWrite;

        //Write a result if there was non-null Intersect Information (unsupported files have null Intersect Information)
        var noIntersections = toWrite.Where(x => x.IntersectInformation != null && !x.IntersectInformation.Tags.Any()).ToList();

        noIntersections.ForEach(x => { x.Result = "No Tags Found"; });

        //Get a list to work with where we have tags to try to write - no null Intersections
        var filteredList = toWrite.Where(x => x.IntersectInformation != null && x.IntersectInformation.Tags.Any()).ToList();

        var exifToolWrites = filteredList.Where(x =>
            FileMetadataTools.ExifToolWriteSupportedExtensions.Any(y =>
                x.FileToTag.Extension.Equals(y, StringComparison.OrdinalIgnoreCase))).ToList();

        var exifTool = FileMetadataTools.ExifToolExecutable(exifToolFullName);
        var frozenExecutionTime = DateTime.Now;

        //Processes a list of tags based on the sanitize, case and length settings - local method
        //so that this can be used with both the intersect and existing tag lists.
        List<string> ProcessTags(List<string> toProcess)
        {
            if (sanitizeTags)
                for (var i = 0; i < toProcess.Count; i++)
                    toProcess[i] =
                        SlugTools.CreateSlug(tagsToLower, toProcess[i], tagMaxCharacterLength);

            if (!sanitizeTags && tagsToLower)
                for (var i = 0; i < toProcess.Count; i++)
                    toProcess[i] = toProcess[i].ToLowerInvariant();

            if (!sanitizeTags)
                for (var i = 0; i < toProcess.Count; i++)
                    toProcess[i] =
                        toProcess[i][
                            ..Math.Min(tagMaxCharacterLength, toProcess[i].Length)];

            return toProcess;
        }

        foreach (var loopWrite in exifToolWrites)
        {
            var existingTags = ProcessTags(await FileMetadataTools.FileKeywords(loopWrite.FileToTag, true));

            loopWrite.ExistingTagString = string.Join(",", existingTags);

            var intersectionTags = ProcessTags(loopWrite.IntersectInformation!.Tags);

            loopWrite.IntersectTagString = string.Join(",", intersectionTags);

            if (intersectionTags.All(x => existingTags.Contains(x, StringComparer.OrdinalIgnoreCase)))
            {
                loopWrite.Result = "No New Tags";
                loopWrite.Notes =
                    $"Intersection Tags - {string.Join(",", intersectionTags)} were all found in the existing tags";
                continue;
            }

            var allTags = existingTags.Union(loopWrite.IntersectInformation!.Tags).OrderBy(x => x).ToList();

            loopWrite.IntersectTagString = string.Join(",", allTags);

            var exifToolKeyword = allTags.Select(x => $"-keywords=\"{x.Replace("\"", "&quot;")}\"").ToList();
            var exifToolSubject = allTags.Select(x => $"-subject=\"{x.Replace("\"", "&quot;")}\"").ToList();
            var exifToolParameters =
                $"-E {string.Join(" ", exifToolKeyword)} {string.Join(" ", exifToolSubject)} -overwrite_original \"{loopWrite.FileToTag.FullName}\"";

            if (testRun)
            {
                loopWrite.Result = "Test Run Success";
                loopWrite.Notes = $"Test Run - would have run ExifTool with {exifToolParameters}";
                continue;
            }

            if (!exifTool.isPresent)
            {
                loopWrite.Result = "ExifTool Not Found";
                loopWrite.Notes = $"Would have run ExifTool with {exifToolParameters}";
                continue;
            }

            if (createBackupBeforeWritingMetadata)
            {
                var backUpSuccessful =
                    UniqueFileTools.WriteFileToBackupDirectory(frozenExecutionTime, "PwFeatureIntersectTag",
                        loopWrite.FileToTag,
                        progress);
                if (!backUpSuccessful)
                {
                    loopWrite.Result = "Backup Error";
                    loopWrite.Notes = "Backup File could not be written - no attempt to write Tags.";
                    progress?.Report(
                        $"GeoTag - Skipping {loopWrite.FileToTag.FullName} - Found Tag information but could not create a backup - skipping this file.");
                    continue;
                }
            }

            try
            {
                var exifToolWriteOutcome =
                    ProcessTools.Execute(exifTool.exifToolFile!.FullName, exifToolParameters, progress);

                if (!exifToolWriteOutcome.success)
                {
                    Log.ForContext("standardOutput", exifToolWriteOutcome.standardOutput)
                        .ForContext("errorOutput", exifToolWriteOutcome.errorOutput)
                        .ForContext("success", exifToolWriteOutcome.success)
                        .ForContext("intersectResults", loopWrite.SafeObjectDump())
                        .ForContext("exifTool", exifTool.SafeObjectDump())
                        .Error($"Writing with ExifTool did not Succeed - {loopWrite.FileToTag.FullName}");

                    loopWrite.Result = "ExifTool Error";
                    loopWrite.Notes = $"ExifTool Reported an Error - {exifToolWriteOutcome.standardOutput}";

                    continue;
                }
            }
            catch (Exception e)
            {
                Log
                    .ForContext("exifToolParameters", exifToolParameters)
                    .ForContext("exifTool", exifTool.SafeObjectDump())
                    .ForContext("intersectResults", loopWrite.SafeObjectDump())
                    .Error(e,
                        $"Error Tagging {loopWrite.FileToTag.FullName} with ExifTool");
                loopWrite.Result = "ExifTool Error";
                loopWrite.Notes = $"ExifTool Reported an Error - {e.Message}";
                continue;
            }

            loopWrite.Result = "Success";
            loopWrite.Notes = "Wrote Tags with ExifTool";
        }


        return toWrite;
    }
}