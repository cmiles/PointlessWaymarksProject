using System;

namespace PointlessWaymarksCmsData.Models
{
    public interface IContentId
    {
        public Guid ContentId { get; }

        public DateTime ContentVersion { get; }
        public int Id { get; }
    }
}