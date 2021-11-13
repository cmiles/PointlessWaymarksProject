namespace PointlessWaymarks.CmsData.Database.Models;

public class GenerationRelatedContent
{
    public Guid ContentOne { get; set; }
    public Guid ContentTwo { get; set; }
    public DateTime GenerationVersion { get; set; }
    public int Id { get; set; }
}