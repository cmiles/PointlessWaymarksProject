namespace PointlessWaymarks.FeatureIntersectionTags.Models;

public record IntersectFileTaggingResult
{
    public IntersectFileTaggingResult(FileInfo fileToTag)
    {
        FileToTag = fileToTag;
    }

    public FileInfo FileToTag { get; }
    public IntersectResult? IntersectInformation { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;

    public string FinalTagString { get; set; }
    public string ExistingTagString { get; set; }
    public string NewTagsString { get; set; }
}