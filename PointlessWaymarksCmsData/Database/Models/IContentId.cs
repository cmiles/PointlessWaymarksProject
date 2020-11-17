using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public interface IContentId
    {
        public Guid ContentId { get; }
        public DateTime ContentVersion { get; }
        public int Id { get; }
    }
}