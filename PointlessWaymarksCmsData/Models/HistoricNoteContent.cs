using System;

namespace PointlessWaymarksCmsData.Models
{
    public class HistoricNoteContent : IContentId, ITag, IBodyContent, ICreatedAndLastUpdateOnAndBy, IShowInSiteFeed
    {
        public int Id { get; set; }
        public Guid ContentId { get; set; }
        public string BodyContent { get; set; }
        public string BodyContentFormat { get; set; }
        public string Tags { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public bool ShowInSiteFeed { get; set; }
        public string Folder { get; set; }
        public string Slug { get; set; }
        public string Summary { get; set; }
    }
}