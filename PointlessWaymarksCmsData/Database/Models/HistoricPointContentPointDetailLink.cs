using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class HistoricPointContentPointDetailLink
    {
        public DateTime ContentVersion { get; set; }
        public int Id { get; set; }
        public Guid PointContentId { get; set; }
        public Guid PointDetailContentId { get; set; }
    }
}