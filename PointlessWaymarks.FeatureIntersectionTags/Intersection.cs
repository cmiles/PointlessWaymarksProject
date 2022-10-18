using System.Reflection.Metadata.Ecma335;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.SpatialTools;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PointlessWaymarks.FeatureIntersectionTags;

public class Intersection
{
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

        var compiledTags = new List<IntersectResults>();

        cancellationToken.ThrowIfCancellationRequested();

        if (settings.IntersectFiles.Any())
            compiledTags.AddRange(TagsFromFileIntersections(toCheck, settings.IntersectFiles.ToList(),
                cancellationToken, progress));

        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(settings.PadUsDirectory)
            && !string.IsNullOrWhiteSpace(settings.PadUsDoiRegionFile)
            && !string.IsNullOrWhiteSpace(settings.PadUsFilePrefix)
            && settings.PadUsAttributesForTags.Any())
            compiledTags.AddRange(TagsFromPadUsIntersections(toCheck, settings.PadUsAttributesForTags.ToList(),
                settings.PadUsDoiRegionFile, settings.PadUsDirectory, settings.PadUsFilePrefix, cancellationToken,
                progress));

        cancellationToken.ThrowIfCancellationRequested();

        return compiledTags.GroupBy(x => x.Feature)
            .Select(x => new IntersectResults(x.Key, x.SelectMany(y => y.Tags).Distinct().ToList())).ToList();
    }

    public List<IntersectResults> TagsFromFileIntersections(List<IFeature> toCheck, List<IntersectFile> intersectFiles,
        CancellationToken cancellationToken,
        IProgress<string>? progress = null)
    {
        var featuresAndTags = toCheck.Select(x => new IntersectResults(x, new List<string>())).ToList();

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
                                var tagValue = (loopIntersectFeature.Attributes[loopAttribute]?.ToString() ??
                                                string.Empty).Trim();
                                if (!loopCheck.Tags.Any(x => x.Equals(tagValue, StringComparison.OrdinalIgnoreCase)))
                                    loopCheck.Tags.Add(tagValue);
                            }

                        if (!string.IsNullOrWhiteSpace(loopIntersectFile.TagAll) && !loopCheck.Tags.Any(x =>
                                x.Equals(loopIntersectFile.TagAll, StringComparison.OrdinalIgnoreCase)))
                            loopCheck.Tags.Add(loopIntersectFile.TagAll);
                    }
                }
            }
        }

        progress?.Report("Returning Features and Tags");

        return featuresAndTags;
    }

    public List<IntersectResults> TagsFromPadUsIntersections(List<IFeature> toCheck, List<string> attributesForTags,
        string padUsRegionFile, string padUsDirectory, string padUsFilePrefix, CancellationToken cancellationToken,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Processing DOI Regions from {padUsRegionFile}");
        var doiRegionsFile = new FileInfo(padUsRegionFile);

        var serializer = GeoJsonSerializer.Create();

        using var doiRegionStringReader =
            new StringReader(
                File.ReadAllText(doiRegionsFile.FullName));
        using var doiRegionJsonReader = new JsonTextReader(doiRegionStringReader);
        var doiRegionFeatures = serializer.Deserialize<FeatureCollection>(doiRegionJsonReader).ToList();

        var regionIntersections = new List<(string? region, IFeature feature)>();

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var loopCheck in toCheck)
        foreach (var loopDoiRegion in doiRegionFeatures)
            if (loopCheck.Geometry.Intersects(loopDoiRegion.Geometry))
                regionIntersections.Add((loopDoiRegion.Attributes["REG_NUM"]?.ToString(), loopCheck));

        var featureTag = new List<(IFeature Feature, string tag)>();

        var counter = 0;

        var regionIntersectionsGroupedByRegion = regionIntersections.GroupBy(x => x.region).ToList();


        foreach (var loopDoiRegionGroup in regionIntersectionsGroupedByRegion)
        {
            cancellationToken.ThrowIfCancellationRequested();

            counter++;

            var regionFile =
                new FileInfo(Path.Combine(padUsDirectory, $"{padUsFilePrefix}{loopDoiRegionGroup.Key}.geojson"));

            progress?.Report(
                $"Processing PADUS DOI Region File - {regionFile.Name} - {counter} of {regionIntersectionsGroupedByRegion.Count}");

            using var regionFileStream =
                File.Open(regionFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var regionStreamReader = new StreamReader(regionFileStream);
            using var regionJsonReader = new JsonTextReader(regionStreamReader);
            var regionFeatures = serializer.Deserialize<FeatureCollection>(regionJsonReader).ToList();

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
                                featureTag.Add((loopCheckFeature, tagValue));
                            }
                }
            }
        }

        progress?.Report("Returning PADUS Features and Tags");

        return toCheck.Select(x =>
                new IntersectResults(x, featureTag.Where(y => y.Feature == x).Select(y => y.tag).Distinct().ToList()))
            .ToList();
    }
}