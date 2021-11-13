using System.ComponentModel.DataAnnotations.Schema;

namespace PointlessWaymarks.CmsData.Database.Models
{
    public class HistoricLinkContent
    {
        public string? Author { get; set; }
        public string? Comments { get; set; }
        public Guid ContentId { get; set; }
        public DateTime ContentVersion { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? Description { get; set; }
        public int Id { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        [NotMapped] public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
        public DateTime? LinkDate { get; set; }
        public bool ShowInLinkRss { get; set; }
        public string? Site { get; set; }
        public string? Tags { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
    }
}