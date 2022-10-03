using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PointlessWaymarks.FeatureIntersectionTags;

public class Intersection
{
    public List<IntersectResults> FindTagsFromIntersections(string intersectSettingsFile,
        List<IFeature> toCheck)
    {
        var settings = JsonSerializer.Deserialize<IntersectSettings>(File.ReadAllText(intersectSettingsFile));

        var boundariesDirectory = new DirectoryInfo(settings.IntersectFilesDirectory);

        var serializer = GeoJsonSerializer.Create();

        var featuresAndTags = toCheck.Select(x => new IntersectResults(x, new List<string>())).ToList();

        foreach (var loopIntersectFile in settings.IntersectFiles)
        {
            using var stringReader =
                new StringReader(
                    File.ReadAllText(Path.Combine(boundariesDirectory.FullName, loopIntersectFile.FileName)));
            using var jsonReader = new JsonTextReader(stringReader);
            var intersectFeatures = serializer.Deserialize<FeatureCollection>(jsonReader).ToList();

            foreach (var loopIntersectFeature in intersectFeatures)
            foreach (var loopCheck in featuresAndTags)
                if (loopCheck.Feature.Geometry.Intersects(loopIntersectFeature.Geometry))
                    foreach (var loopAttribute in loopIntersectFile.AttributesForTags)
                        if (loopIntersectFeature.Attributes.GetNames().Any(a => a == loopAttribute))
                        {
                            var tagValue = (loopIntersectFeature.Attributes[loopAttribute]?.ToString() ??
                                            string.Empty).Trim();
                            if (!loopCheck.Tags.Contains(tagValue)) loopCheck.Tags.Add(tagValue);

                            if (!string.IsNullOrWhiteSpace(loopIntersectFile.TagAll))
                                loopCheck.Tags.Add(loopIntersectFile.TagAll);
                        }
        }

        return featuresAndTags;
    }
}