using System.ComponentModel.DataAnnotations.Schema;

namespace PointlessWaymarks.CmsData.Database.Models;

public class LinkContent : ICreatedAndLastUpdateOnAndBy, ITag, IContentId
{
    public string? Author { get; set; }
    public string? Comments { get; set; }
    public string? Description { get; set; }
    public DateTime? LinkDate { get; set; }
    public bool ShowInLinkRss { get; set; }
    public string? Site { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public required Guid ContentId { get; set; }
    public required DateTime ContentVersion { get; set; }
    public int Id { get; set; }
    public string? CreatedBy { get; set; }
    public required DateTime CreatedOn { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    [NotMapped] public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public string? Tags { get; set; }

    public static LinkContent CreateInstance()
    {
        return NewContentModels.InitializeLinkContent(null);
    }
}