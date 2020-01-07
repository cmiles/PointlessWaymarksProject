using System;

namespace PointlessWaymarksCmsData.Models
{
    public interface IContentId
    {
        public Guid ContentId { get; set; }
        public int Id { get; set; }
    }
}