using System;

namespace PointlessWaymarksCmsData.Models
{
    public interface ICreatedAndLastUpdateOnAndBy
    {
        public string CreatedBy { get; }
        public DateTime CreatedOn { get; }
        public string LastUpdatedBy { get; }
        public DateTime? LastUpdatedOn { get; }
    }
}