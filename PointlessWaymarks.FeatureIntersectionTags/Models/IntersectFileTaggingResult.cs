namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public record IntersectFileTaggingResult
{
    public IntersectFileTaggingResult(FileInfo fileToTag)
    {
        FileToTag = fileToTag;
    }

    public FileInfo FileToTag { get; }
    public IntersectResult? Intersections { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Results { get; set; } = string.Empty;
}