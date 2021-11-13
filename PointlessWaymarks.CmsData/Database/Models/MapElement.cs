namespace PointlessWaymarks.CmsData.Database.Models;

public class MapElement
{
    public Guid ElementContentId { get; set; }
    public int Id { get; set; }
    public bool IncludeInDefaultView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public Guid MapComponentContentId { get; set; }
    public bool ShowDetailsDefault { get; set; }
}