using System.ComponentModel.DataAnnotations.Schema;

namespace PointlessWaymarks.CmsData.Database.Models;

public class TrailContent : IUpdateNotes, IContentCommon
{
    public string? Bikes { get; set; }
    public string? BikesNote { get; set; }
    public string? Dogs { get; set; }
    public string? DogsNote { get; set; }
    public Guid? EndingPointContentId { get; set; }
    public string? Fee { get; set; }
    public string? FeeNote { get; set; }
    public Guid? LineContentId { get; set; }
    public string? LocationArea { get; set; }
    public Guid? MapComponentId { get; set; }
    public string? OtherDetails { get; set; }
    public Guid? StartingPointContentId { get; set; }
    public string? TrailShape { get; set; }
    public string? BodyContent { get; set; }
    public string? BodyContentFormat { get; set; }
    public required Guid ContentId { get; set; }
    public required DateTime ContentVersion { get; set; }
    public int Id { get; set; }
    public string? CreatedBy { get; set; }
    public required DateTime CreatedOn { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    [NotMapped] public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public Guid? MainPicture { get; set; }
    public required DateTime FeedOn { get; set; }
    public bool IsDraft { get; set; }
    public bool ShowInMainSiteFeed { get; set; }
    public bool ShowInSearch { get; set; } = true;
    public string? Tags { get; set; }
    public string? Title { get; set; }
    public string? Folder { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }

    public static TrailContent CreateInstance()
    {
        return NewContentModels.InitializeTrailContent(null);
    }
}