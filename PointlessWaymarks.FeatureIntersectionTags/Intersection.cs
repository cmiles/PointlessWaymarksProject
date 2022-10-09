using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using System.Collections.Generic;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PointlessWaymarks.FeatureIntersectionTags;

public class Intersection
{
    public List<IntersectResults> Tags(string intersectSettingsFile,
        List<IFeature> toCheck, IProgress<string>? progress = null)
    {
        progress?.Report($"Getting Settings from {intersectSettingsFile}");
        var settings = JsonSerializer.Deserialize<IntersectSettings>(File.ReadAllText(intersectSettingsFile));

        var compiledTags = new List<IntersectResults>();

        if (settings.IntersectFiles.Any())
        {
            compiledTags.AddRange(TagsFromFileIntersections(toCheck, settings.IntersectFiles.ToList(), progress));
        }

        if (!string.IsNullOrWhiteSpace(settings.PadUsDirectory)
            && !string.IsNullOrWhiteSpace(settings.PadUsDoiRegionFile)
            && !string.IsNullOrWhiteSpace(settings.PadUsFilePrefix)
            && settings.PadUsAttributesForTags.Any())
        {
            compiledTags.AddRange(TagsFromPadUsIntersections(toCheck, settings.PadUsAttributesForTags.ToList(), settings.PadUsDoiRegionFile, settings.PadUsDirectory, settings.PadUsFilePrefix, progress));
        }

        return compiledTags.GroupBy(x => x.Feature).Select(x => new IntersectResults(x.Key, x.SelectMany(y => y.Tags).Distinct().ToList())).ToList();
    }

    public List<IntersectResults> TagsFromFileIntersections(List<IFeature> toCheck, List<IntersectFile> intersectFiles, IProgress<string>? progress = null)
    {
        var serializer = GeoJsonSerializer.Create();

        var featuresAndTags = toCheck.Select(x => new IntersectResults(x, new List<string>())).ToList();

        var counter = 0;
        foreach (var loopIntersectFile in intersectFiles)
        {
            counter++;

            progress?.Report(
                $"Processing Feature Intersect - {loopIntersectFile.Name}, {loopIntersectFile.FileName} - {counter} of {intersectFiles.Count}");

            using var stringReader =
                new StringReader(
                    File.ReadAllText(loopIntersectFile.FileName));

            using var intersectFileStream = File.Open(loopIntersectFile.FileName, FileMode.Open);
            using var intersectStreamReader = new StreamReader(intersectFileStream);
            using var intersectJsonReader = new JsonTextReader(intersectStreamReader);
            var intersectFeatures = serializer.Deserialize<FeatureCollection>(intersectJsonReader).ToList();

            progress?.Report($" Processing {intersectFeatures.Count} Features...");
            foreach (var loopIntersectFeature in intersectFeatures)
            foreach (var loopCheck in featuresAndTags)
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

        progress?.Report("Returning Features and Tags");

        return featuresAndTags;
    }

    public List<IntersectResults> TagsFromPadUsIntersections(List<IFeature> toCheck, List<string> attributesForTags,
        string padUsRegionFile, string padUsDirectory, string padUsFilePrefix, IProgress<string>? progress = null)
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

        foreach (var loopCheck in toCheck)
        foreach (var loopDoiRegion in doiRegionFeatures)
            if (loopCheck.Geometry.Intersects(loopDoiRegion.Geometry))
                regionIntersections.Add((loopDoiRegion.Attributes["REG_NUM"]?.ToString(), loopCheck));

        var featureTag = new List<(IFeature Feature, string tag)>();

        var counter = 0;

        var regionIntersectionsGroupedByRegion = regionIntersections.GroupBy(x => x.region).ToList();

        foreach (var loopDoiRegionGroup in regionIntersectionsGroupedByRegion)
        {
            counter++;

            var regionFile =
                new FileInfo(Path.Combine(padUsDirectory, $"{padUsFilePrefix}{loopDoiRegionGroup.Key}.geojson"));

            progress?.Report(
                $"Processing PADUS DOI Region File - {regionFile.Name} - {counter} of {regionIntersectionsGroupedByRegion.Count}");

            using var regionFileStream = File.Open(regionFile.FullName, FileMode.Open);
            using var regionStreamReader = new StreamReader(regionFileStream);
            using var regionJsonReader = new JsonTextReader(regionStreamReader);
            var regionFeatures = serializer.Deserialize<FeatureCollection>(regionJsonReader).ToList();

            foreach (var loopRegionFeature in regionFeatures)
            foreach (var loopCheckFeature in toCheck)
                if (loopCheckFeature.Geometry.Intersects(loopRegionFeature.Geometry))
                    foreach (var loopAttribute in attributesForTags)
                        if (loopRegionFeature.Attributes.GetNames().Any(a => a == loopAttribute))
                        {
                            var tagValue = (loopRegionFeature.Attributes[loopAttribute]?.ToString() ??
                                            string.Empty).Trim();
                            featureTag.Add((loopCheckFeature, tagValue));
                        }
        }

        progress?.Report("Returning PADUS Features and Tags");

        return toCheck.Select(x =>
                new IntersectResults(x, featureTag.Where(y => y.Feature == x).Select(y => y.tag).Distinct().ToList()))
            .ToList();
    }
}