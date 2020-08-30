using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class HistoricPointDetail : IContentId, ICreatedAndLastUpdateOnAndBy
    {
        public Guid ContentId { get; set; }
        public DateTime ContentVersion { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string DataType { get; set; }
        public int Id { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string StructuredDataAsJson { get; set; }
    }
}