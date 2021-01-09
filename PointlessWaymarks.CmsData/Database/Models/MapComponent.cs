using System;

namespace PointlessWaymarks.CmsData.Database.Models
{
    public class MapComponent : IContentId, ICreatedAndLastUpdateOnAndBy, IUpdateNotes
    {
        public double InitialViewBoundsMaxLatitude { get; set; }
        public double InitialViewBoundsMaxLongitude { get; set; }
        public double InitialViewBoundsMinLatitude { get; set; }
        public double InitialViewBoundsMinLongitude { get; set; }
        public string Summary { get; set; }
        public string Title { get; set; }
        public Guid ContentId { get; set; }
        public DateTime ContentVersion { get; set; }
        public int Id { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string UpdateNotes { get; set; }
        public string UpdateNotesFormat { get; set; }
    }
}