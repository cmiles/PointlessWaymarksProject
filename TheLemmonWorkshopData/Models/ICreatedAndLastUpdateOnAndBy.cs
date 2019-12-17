using System;

namespace TheLemmonWorkshopData.Models
{
    public interface ICreatedAndLastUpdateOnAndBy
    {
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }
}