using System.Text.Json;
using System.Text.RegularExpressions;
using NetTopologySuite.Features;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.FeatureIntersectionTags;

public class Intersection
{
    public List<IntersectResults> Tags(IntersectSettings settings,
        List<IFeature> toCheck, CancellationToken cancellationToken, IProgress<string>? progress = null)
    {
        var compiledTags = new List<IntersectResults>();

        cancellationToken.ThrowIfCancellationRequested();

        if (settings.IntersectFiles.Any())
            compiledTags.AddRange(TagsFromFileIntersections(toCheck, settings.IntersectFiles.ToList(),
                cancellationToken, progress));

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(settings.PadUsDirectory) && settings.PadUsAttributesForTags.Any())
            compiledTags.AddRange(TagsFromPadUsIntersections(toCheck, settings.PadUsAttributesForTags.ToList(),
                settings.PadUsDirectory, cancellationToken,
                progress));

        cancellationToken.ThrowIfCancellationRequested();

        return compiledTags.GroupBy(x => x.Feature)
            .Select(x => new IntersectResults(x.Key, x.SelectMany(y => y.Tags).Distinct().ToList(),
                x.SelectMany(y => y.IntersectsWith).ToList())).ToList();
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
    public List<IntersectResults> Tags(string intersectSettingsFile,
        List<IFeature> toCheck, CancellationToken cancellationToken, IProgress<string>? progress = null)
    {
        if (string.IsNullOrEmpty(intersectSettingsFile))
        {
            progress?.Report("No Settings File Submitted - returning nothing...");

            return new List<IntersectResults>();
        }

        if (!toCheck.Any())
        {
            progress?.Report("No Features to Check - returning nothing...");

            return new List<IntersectResults>();
        }

        progress?.Report($"Getting Settings from {intersectSettingsFile}");
        var settings = JsonSerializer.Deserialize<IntersectSettings>(File.ReadAllText(intersectSettingsFile));

        if (settings == null)
        {
            progress?.Report($"The settings file {intersectSettingsFile} did not deserialized to valid settings...");

            return new List<IntersectResults>();
        }

        return Tags(settings, toCheck, cancellationToken, progress);
    }

    public List<IntersectResults> TagsFromFileIntersections(List<IFeature> toCheck, List<FeatureFile> intersectFiles,
        CancellationToken cancellationToken,
        IProgress<string>? progress = null)
    {
        var featuresAndTags = toCheck.Select(x => new IntersectResults(x, new List<string>(), new List<IFeature>()))
            .ToList();

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
                $" Processing {intersectFeatures.Count} Reference Features against {featuresAndTags.Count} Submitted Features");

            foreach (var loopIntersectFeature in intersectFeatures)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (++referenceFeatureCounter % 1000 == 0)
                    progress?.Report(
                        $" Processing {loopIntersectFile.Name} - Feature {referenceFeatureCounter} of {intersectFeatures.Count}");

                foreach (var loopCheck in featuresAndTags)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (loopCheck.Feature.Geometry.Intersects(loopIntersectFeature.Geometry))
                    {
                        foreach (var loopAttribute in loopIntersectFile.AttributesForTags)
                            if (loopIntersectFeature.Attributes.GetNames().Any(a => a == loopAttribute))
                            {
                                loopCheck.IntersectsWith.Add(loopIntersectFeature);

                                var tagValue = (loopIntersectFeature.Attributes[loopAttribute]?.ToString() ??
                                                string.Empty).Trim();
                                if (!loopCheck.Tags.Any(x => x.Equals(tagValue, StringComparison.OrdinalIgnoreCase)))
                                    loopCheck.Tags.Add(tagValue);
                            }

                        if (!string.IsNullOrWhiteSpace(loopIntersectFile.TagAll) && !loopCheck.Tags.Any(x =>
                                x.Equals(loopIntersectFile.TagAll, StringComparison.OrdinalIgnoreCase)))
                        {
                            loopCheck.IntersectsWith.Add(loopIntersectFeature);

                            loopCheck.Tags.Add(loopIntersectFile.TagAll);
                        }
                    }
                }
            }
        }

        progress?.Report("Returning Features and Tags");

        return featuresAndTags;
    }

    public List<IntersectResults> TagsFromPadUsIntersections(List<IFeature> toCheck, List<string> attributesForTags,
        string padUsDirectory, CancellationToken cancellationToken,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(padUsDirectory)) return new List<IntersectResults>();

        var padUsDirectoryInfo = new DirectoryInfo(padUsDirectory);

        if (!padUsDirectoryInfo.Exists)
        {
            progress?.Report($"PAD-US directory {padUsDirectory} doesn't exist...");
            return new List<IntersectResults>();
        }

        var regionsFile = padUsDirectoryInfo.EnumerateFiles("*Regions.geojson", SearchOption.TopDirectoryOnly).ToList();

        if (regionsFile.Count != 1)
        {
            progress?.Report(
                $"Couldn't find a single Region file matching *Regions.geojson in {padUsDirectoryInfo.FullName}");
            return new List<IntersectResults>();
        }

        var doiRegionsFile = regionsFile.First();

        var doiRegionFeatures = GeoJsonTools.DeserializeFileToFeatureCollection(doiRegionsFile.FullName);

        var regionIntersections = new List<(string? region, IFeature feature)>();

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var loopCheck in toCheck)
        foreach (var loopDoiRegion in doiRegionFeatures)
            if (loopCheck.Geometry.Intersects(loopDoiRegion.Geometry))
                regionIntersections.Add((loopDoiRegion.Attributes["REG_NUM"]?.ToString(), loopCheck));

        var featureTag = new List<(IFeature featureToTag, string tag, IFeature intersectsWith)>();

        var counter = 0;

        var regionIntersectionsGroupedByRegion = regionIntersections.GroupBy(x => x.region).ToList();

        var geoJsonFiles = padUsDirectoryInfo.EnumerateFiles("*.geojson", SearchOption.TopDirectoryOnly).ToList();

        var regionFiles = geoJsonFiles.Where(x =>
                Regex.IsMatch(x.Name, ".*[0-9]{1,2}.geojson", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
            .ToList();

        foreach (var loopDoiRegionGroup in regionIntersectionsGroupedByRegion)
        {
            cancellationToken.ThrowIfCancellationRequested();

            counter++;

            var regionFile = geoJsonFiles.FirstOrDefault(x =>
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

            foreach (var loopRegionFeature in regionFeatures)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (++referenceFeatureCounter % 5000 == 0)
                    progress?.Report(
                        $" Processing {regionFile.Name} - Feature {referenceFeatureCounter} of {regionFeatures.Count}");

                foreach (var loopCheckFeature in toCheck)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (loopCheckFeature.Geometry.Intersects(loopRegionFeature.Geometry))
                        foreach (var loopAttribute in attributesForTags)
                            if (loopRegionFeature.Attributes.GetNames().Any(a => a == loopAttribute))
                            {
                                var tagValue = (loopRegionFeature.Attributes[loopAttribute]?.ToString() ??
                                                string.Empty).Trim();
                                featureTag.Add((loopCheckFeature, tagValue, loopRegionFeature));
                            }
                }
            }
        }

        progress?.Report("Returning PADUS Features and Tags");

        return toCheck.Select(x =>
                new IntersectResults(x,
                    featureTag.Where(y => y.featureToTag == x).Select(y => y.tag).Distinct().ToList(),
                    featureTag.Where(y => y.featureToTag == x).Select(y => y.intersectsWith).ToList()))
            .ToList();
    }
}