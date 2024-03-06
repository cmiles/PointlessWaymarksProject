namespace PointlessWaymarks.CmsData.Database.Models;

public class MapIcon
{
    public required Guid ContentId { get; set; }
    public required DateTime ContentVersion { get; set; }
    public string? IconName { get; set; }
    public string? IconSource { get; set; }
    public string? IconSvg { get; set; }
    public int Id { get; set; }
    public string? LastUpdatedBy { get; set; }
    public required DateTime LastUpdatedOn { get; set; }

    public HistoricMapIcon ToHistoricMapIcon()
    {
        return new()
        {
            ContentId = ContentId,
            ContentVersion = ContentVersion,
            IconName = IconName,
            IconSource = IconSource,
            IconSvg = IconSvg,
            LastUpdatedBy = LastUpdatedBy ?? "Historic Content Archivist",
            LastUpdatedOn = LastUpdatedOn
        };
    }
}