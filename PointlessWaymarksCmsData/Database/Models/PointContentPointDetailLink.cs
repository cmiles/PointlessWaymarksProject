using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class PointContentPointDetailLink : ICreatedAndLastUpdateOnAndBy
    {
        public DateTime ContentVersion { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int Id { get; set; }

        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }

        public Guid PointContentId { get; set; }

        public Guid PointDetailContentId { get; set; }
    }
}