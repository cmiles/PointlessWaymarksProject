using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class PointContentPointDetailLink
    {
        public DateTime ContentVersion { get; set; }
        public int Id { get; set; }
        public Guid PointContentId { get; set; }
        public Guid PointDetailContentId { get; set; }
    }
}