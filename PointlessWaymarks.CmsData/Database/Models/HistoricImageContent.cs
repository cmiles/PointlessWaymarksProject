using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PointlessWaymarks.CmsData.Database.Models
{
    public class HistoricImageContent : IUpdateNotes, IContentCommon, IShowInSearch
    {
        public string? AltText { get; set; }
        public string? OriginalFileName { get; set; }
        public string? BodyContent { get; set; }
        public string? BodyContentFormat { get; set; }
        public Guid ContentId { get; set; }
        public DateTime ContentVersion { get; set; }
        public int Id { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        [NotMapped] public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
        public Guid? MainPicture { get; set; }
        public DateTime FeedOn { get; set; }
        public bool IsDraft { get; set; }
        public bool ShowInMainSiteFeed { get; set; }
        public bool ShowInSearch { get; set; }
        public string? Tags { get; set; }
        public string? Folder { get; set; }
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        public string? Title { get; set; }
        public string? UpdateNotes { get; set; }
        public string? UpdateNotesFormat { get; set; }
    }
}