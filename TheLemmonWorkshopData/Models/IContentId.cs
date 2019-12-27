using System;

namespace TheLemmonWorkshopData.Models
{
    public interface IContentId
    {
        public Guid ContentId { get; set; }
        public int Id { get; set; }
    }
}