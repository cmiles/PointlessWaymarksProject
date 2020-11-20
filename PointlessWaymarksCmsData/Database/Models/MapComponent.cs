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
        public double InitialViewBoundsLowerRightLatitude { get; set; }
        public double InitialViewBoundsLowerRightLongitude { get; set; }
        public double InitialViewBoundsUpperLeftLatitude { get; set; }
        public double InitialViewBoundsUpperLeftLongitude { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string Summary { get; set; }
        public string Title { get; set; }
        public string UpdateNotes { get; set; }
        public string UpdateNotesFormat { get; set; }
    }
}