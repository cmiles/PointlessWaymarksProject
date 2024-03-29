namespace PointlessWaymarks.CmsData.Database.Models;

public class HistoricPointDetail : IContentId
{
    public Guid ContentId { get; set; }
    public DateTime ContentVersion { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? DataType { get; set; }
    public Guid HistoricGroupId { get; set; }
    public int Id { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    public Guid PointContentId { get; set; }
    public string? StructuredDataAsJson { get; set; }
}