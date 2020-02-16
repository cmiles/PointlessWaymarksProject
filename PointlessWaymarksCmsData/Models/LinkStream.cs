using System;

namespace PointlessWaymarksCmsData.Models
{
    public class LinkStream : ICreatedAndLastUpdateOnAndBy, ITag, IContentId
    {
        public string Author { get; set; }
        public string Comments { get; set; }

        public string Description { get; set; }

        public string Site { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public int Id { get; set; }

        public Guid ContentId { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string Tags { get; set; }
    }
}