namespace PointlessWaymarks.CmsData.Database.Models;

public interface ITitleSummarySlugFolder : ITitle
{
    public string? Folder { get; }
    public string? Slug { get; }
    public string? Summary { get; }
}