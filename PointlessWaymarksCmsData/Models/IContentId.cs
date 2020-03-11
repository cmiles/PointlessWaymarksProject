using System;

namespace PointlessWaymarksCmsData.Models
{
    public interface IContentId
    {
        public Guid ContentId { get; }
        public int Id { get; }
    }
}