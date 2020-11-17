using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class MapComponent : IContentId, ICreatedAndLastUpdateOnAndBy, IUpdateNotes
    {
        public Guid ContentId { get; set; }
        public DateTime ContentVersion { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int Id { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string Summary { get; set; }
        public string UpdateNotes { get; set; }
        public string UpdateNotesFormat { get; set; }
    }
}