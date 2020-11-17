using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class MapComponentElement
    {
        public int Id { get; set; }
        public Guid ElementId { get; set; }
        public bool InitialFocus { get; set; }
        public Guid MapComponentContentId { get; set; }
    }
}