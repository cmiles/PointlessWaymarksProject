namespace PointlessWaymarks.CmsData.Database.Models;

public class GenerationTagLog
{
    public DateTime GenerationVersion { get; set; }
    public int Id { get; set; }
    public Guid RelatedContentId { get; set; }
    public bool TagIsExcludedFromSearch { get; set; }
    public string? TagSlug { get; set; }
}