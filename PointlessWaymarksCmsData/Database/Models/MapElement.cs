using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class MapElement
    {
        public Guid ContentId { get; set; }
        public int Id { get; set; }
        public bool IncludeInDefaultView { get; set; }
        public Guid MapComponentContentId { get; set; }
        public bool ShowDetailsDefault { get; set; }
    }
}