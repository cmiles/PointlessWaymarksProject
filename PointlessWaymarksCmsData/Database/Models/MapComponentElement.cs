using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public class MapComponentElement
    {
        public Guid ElementContentId { get; set; }
        public int Id { get; set; }
        public bool InitialDetails { get; set; }
        public Guid MapComponentContentId { get; set; }
    }
}