using System;

namespace TheLemmonWorkshopData.Models
{
    public interface IContentId
    {
        public int Id { get; set; }

        public Guid Fingerprint { get; set; }
    }
}