namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public record IntersectFileTaggingResult
{
    public IntersectFileTaggingResult(FileInfo fileToTag)
    {
        FileToTag = fileToTag;
    }

    public readonly FileInfo FileToTag;
    public string Results = String.Empty;
    public string Notes = string.Empty;
    public List<IntersectResults> Intersections = new();
}