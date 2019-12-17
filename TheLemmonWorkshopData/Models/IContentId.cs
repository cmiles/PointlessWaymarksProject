using System;

namespace TheLemmonWorkshopData.Models
{
    public interface IContentId
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public Guid Fingerprint { get; set; }
    }
}