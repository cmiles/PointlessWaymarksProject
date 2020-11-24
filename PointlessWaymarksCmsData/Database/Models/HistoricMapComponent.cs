using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class HistoricMapComponent : IContentId, ICreatedAndLastUpdateOnAndBy, IUpdateNotes
    {
        public Guid ContentId { get; set; }
        public DateTime ContentVersion { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int Id { get; set; }
        public double InitialViewBoundsMaxX { get; set; }
        public double InitialViewBoundsMaxY { get; set; }
        public double InitialViewBoundsMinX { get; set; }
        public double InitialViewBoundsMinY { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string Summary { get; set; }
        public string Title { get; set; }
        public string UpdateNotes { get; set; }
        public string UpdateNotesFormat { get; set; }
    }
}