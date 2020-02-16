using System;

namespace PointlessWaymarksCmsData.Models
{
    public class HistoricLinkStream
    {
        public string Comments { get; set; }

        public Guid ContentId { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ExtractedData { get; set; }
        public int Id { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string Tags { get; set; }
        public string Url { get; set; }
    }
}