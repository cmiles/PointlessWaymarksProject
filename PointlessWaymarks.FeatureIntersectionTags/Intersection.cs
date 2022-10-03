using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PointlessWaymarks.FeatureIntersectionTags;

public class Intersection
{
    public List<IntersectResults> FindTagsFromIntersections(string intersectSettingsFile,
        List<IFeature> toCheck, IProgress<string>? progress = null)
    {
        progress?.Report($"Getting Settings from {intersectSettingsFile}");
        var settings = JsonSerializer.Deserialize<IntersectSettings>(File.ReadAllText(intersectSettingsFile));

        var serializer = GeoJsonSerializer.Create();

        var featuresAndTags = toCheck.Select(x => new IntersectResults(x, new List<string>())).ToList();

        var counter = 0;
        foreach (var loopIntersectFile in settings.IntersectFiles)
        {
            counter++;

            progress?.Report(
                $"Processing Feature Intersect - {loopIntersectFile.Name}, {loopIntersectFile.FileName} - {counter} of {settings.IntersectFiles.Count}");

            using var stringReader =
                new StringReader(
                    File.ReadAllText(loopIntersectFile.FileName));
            using var jsonReader = new JsonTextReader(stringReader);
            var intersectFeatures = serializer.Deserialize<FeatureCollection>(jsonReader).ToList();

            progress?.Report($" Processing {intersectFeatures.Count} Features...");
            foreach (var loopIntersectFeature in intersectFeatures)
            foreach (var loopCheck in featuresAndTags)
                if (loopCheck.Feature.Geometry.Intersects(loopIntersectFeature.Geometry))
                {
                    foreach (var loopAttribute in loopIntersectFile.AttributesForTags)
                    {
                        if (loopIntersectFeature.Attributes.GetNames().Any(a => a == loopAttribute))
                        {
                            var tagValue = (loopIntersectFeature.Attributes[loopAttribute]?.ToString() ??
                                            string.Empty).Trim();
                            if (!loopCheck.Tags.Any(x => x.Equals(tagValue, StringComparison.OrdinalIgnoreCase)))
                                loopCheck.Tags.Add(tagValue);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(loopIntersectFile.TagAll) && !loopCheck.Tags.Any(x =>
                            x.Equals(loopIntersectFile.TagAll, StringComparison.OrdinalIgnoreCase)))
                        loopCheck.Tags.Add(loopIntersectFile.TagAll);
                }
        }

        progress?.Report("Returning Features and Tags");

        return featuresAndTags;
    }
}