﻿using System.ComponentModel.DataAnnotations.Schema;

namespace PointlessWaymarks.CmsData.Database.Models;

public class VideoContent : IUpdateNotes, IContentCommon
{
    public string? BodyContent { get; set; }
    public string? BodyContentFormat { get; set; }
    public required Guid ContentId { get; set; }
    public required DateTime ContentVersion { get; set; }
    public string? CreatedBy { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required DateTime FeedOn { get; set; }
    public string? Folder { get; set; }
    public int Id { get; set; }
    public bool IsDraft { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    [NotMapped] public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
    public string? License { get; set; }
    public Guid? MainPicture { get; set; }
    public string? OriginalFileName { get; set; }
    public bool ShowInMainSiteFeed { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Tags { get; set; }
    public string? Title { get; set; }
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }
    public Guid? UserMainPicture { get; set; }
    public string? VideoCreatedBy { get; set; }
    public required DateTime VideoCreatedOn { get; set; }
    public DateTime? VideoCreatedOnUtc { get; set; }

    public static VideoContent CreateInstance()
    {
        return NewContentModels.InitializeVideoContent(null);
    }
}