using System.Text.Json;
using System.Text.RegularExpressions;
using NetTopologySuite.Features;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.LoggingTools;
using PointlessWaymarks.SpatialTools;

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
                FileMetadataTools.TagSharpAndExifToolSupportedExtensions.Any(y =>
                    y.Equals(x.FileToTag.Extension, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var loopFile in metadataFiles)
        {
            var location = await FileMetadataTools.Location(loopFile.FileToTag, false, progress);

            if (!location.HasValidLocation()) continue;

            var feature = new Feature(PointTools.Wgs84Point(location.Longitude!.Value, location.Latitude!.Value),
                new AttributesTable());

            loopFile.Intersections = new IntersectResults(feature);
        }

        var gpxFiles = sourceFileAndFeatures.Where(x =>
            x.FileToTag.Extension.Equals(".GPX")).ToList();

        foreach (var loopGpx in gpxFiles)
        {
            var trackLines = await GpxTools.TrackLinesFromGpxFile(loopGpx.FileToTag);
            var routeLines = await GpxTools.RouteLinesFromGpxFile(loopGpx.FileToTag);
            var waypointPoints = await GpxTools.WaypointPointsFromGpxFile(loopGpx.FileToTag);

            loopGpx.Intersections = new IntersectResults(trackLines.features.Cast<IFeature>().Union(routeLines.features)
                .Union(waypointPoints.features).ToList());
        }

        var geojsonFiles = sourceFileAndFeatures.Where(x =>
            x.FileToTag.Extension.Equals(".GEOJSON")).ToList();

        foreach (var loopGeojson in geojsonFiles)
        {
            var features = GeoJsonTools.DeserializeFileToFeatureCollection(loopGeojson.FileToTag.FullName).ToList();

            foreach (var loopFeature in features) loopFeature.Attributes.Add("title", loopGeojson.FileToTag.Name);

            loopGeojson.Intersections = new IntersectResults(features);
        }

        sourceFileAndFeatures.Where(x => x.Intersections != null).Select(x => x.Intersections!).ToList()
            .IntersectionTags(settings,
                cancellationToken,
                progress);

        return sourceFileAndFeatures;
    }

    public static List<IntersectResults> IntersectionTags(this List<IntersectResults> toCheck,
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
    public static List<IntersectResults> IntersectionTags(this List<IntersectResults> toCheck,
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

        var intersectionResult = new IntersectResults(toCheck);

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

    public static IntersectResults IntersectionTags(this IntersectResults toCheck, string intersectSettingsFile,
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

    public static List<IntersectResults> ProcessFileIntersections(this List<IntersectResults> toCheck,
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

    public static List<IntersectResults> ProcessPadUsIntersections(this List<IntersectResults> toCheck,
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
            .Where(x => x.Name.EndsWith("*Regions.geojson", StringComparison.OrdinalIgnoreCase)).ToList();

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

        var regionIntersections = new List<(string? region, IntersectResults feature)>();

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
                $"Processing PADUS DOI Region File - {regionFile.Name} - {counter} of {regionIntersectionsGroupedByRegion.Count}");

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
        if (!toWrite.Any(x => x.Intersections != null && x.Intersections.Tags.Any())) return toWrite;

        //Write results for items with no Metadata or no Tags
        var nullIntersections = toWrite.Where(x => x.Intersections == null).ToList();

        nullIntersections.ForEach(x => { x.Results = "No GeoLocation Found"; });

        var noIntersections = toWrite.Where(x => x.Intersections == null || !x.Intersections.Tags.Any()).ToList();

        noIntersections.ForEach(x => { x.Results = "No Tags Found"; });

        //Get a list to work with where we have tags to try to write - no null Intersections
        var filteredList = toWrite.Where(x => x.Intersections != null && x.Intersections.Tags.Any()).ToList();

        //Take care of Tag Processing - this slightly awkward if stack is in place because sanitize tags uses
        //a method that also takes care of lower case and length
        if (sanitizeTags)
            foreach (var loopList in filteredList)
                for (var i = 0; i < loopList.Intersections!.Tags.Count; i++)
                    loopList.Intersections.Tags[i] =
                        SlugTools.CreateSlug(tagsToLower, loopList.Intersections.Tags[i], tagMaxCharacterLength);

        if (!sanitizeTags && tagsToLower)
            foreach (var loopList in filteredList)
                for (var i = 0; i < loopList.Intersections!.Tags.Count; i++)
                    loopList.Intersections.Tags[i] = loopList.Intersections.Tags[i].ToLowerInvariant();

        if (!sanitizeTags)
            foreach (var loopList in filteredList)
                for (var i = 0; i < loopList.Intersections!.Tags.Count; i++)
                    loopList.Intersections.Tags[i] =
                        loopList.Intersections.Tags[i][
                            ..Math.Min(tagMaxCharacterLength, loopList.Intersections.Tags[i].Length)];

        var exifToolWrites = filteredList.Where(x =>
            FileMetadataTools.ExifToolWriteSupportedExtensions.Any(y =>
                x.FileToTag.Extension.Equals(y, StringComparison.OrdinalIgnoreCase))).ToList();

        var exifToolCheck = FileMetadataTools.ExifToolExecutable(exifToolFullName);

        foreach (var loopTagSharpWrite in exifToolWrites)
        {
        }

        return toWrite;
    }
}