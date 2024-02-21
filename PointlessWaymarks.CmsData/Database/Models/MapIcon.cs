namespace PointlessWaymarks.CmsData.Database.Models;

public class MapIcon
{
    public required Guid ContentId { get; set; }
    public string? IconName { get; set; }
    public string? IconSource { get; set; }
    public string? IconSvg { get; set; }
    public int Id { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
}