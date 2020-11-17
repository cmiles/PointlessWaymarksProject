using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class HistoricMapComponentElement
    {
        public DateTime ContentVersion { get; set; }
        public string ElementsJson { get; set; }
        public int Id { get; set; }
        public Guid MapComponentContentId { get; set; }
    }
}