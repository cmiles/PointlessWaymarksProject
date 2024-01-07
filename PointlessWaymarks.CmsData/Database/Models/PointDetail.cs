namespace PointlessWaymarks.CmsData.Database.Models;

public class PointDetail : IContentId
{
    public required Guid ContentId { get; set; }
    public required DateTime ContentVersion { get; set; }
    public required DateTime CreatedOn { get; set; }
    public string? DataType { get; set; }
    public int Id { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    public Guid PointContentId { get; set; }
    public string? StructuredDataAsJson { get; set; }

    public static PointDetail CreateInstance()
    {
        return NewContentModels.InitializePointDetail(null);
    }

    public static PointDetail CreateInstance(Guid pointContentId)
    {
        var newPointDetail = NewContentModels.InitializePointDetail(null);
        newPointDetail.PointContentId = pointContentId;
        return newPointDetail;
    }
}