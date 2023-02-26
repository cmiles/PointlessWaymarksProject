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
    public string FinalTagString { get; set; } = string.Empty;
    public string ExistingTagString { get; set; } = string.Empty;
    public string NewTagsString { get; set; } = string.Empty;
}