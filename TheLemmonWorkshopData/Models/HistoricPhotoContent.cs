using System;

namespace TheLemmonWorkshopData.Models
{
    public class HistoricPhotoContent
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public string AltText { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Camera { get; set; }
        public string Lens { get; set; }
        public string Aperture { get; set; }
        public string ShutterSpeed { get; set; }
        public string BaseFileName { get; set; }
        public DateTime PhotoCreatedOn { get; set; }
        public DateTime PageCreatedOn { get; set; }
        public DateTime PageLastUpdateOn { get; set; }
        public DateTime PageLastUpdateBy { get; set; }
        public Guid Fingerprint { get; set; }
        public int Id { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string UpdateNotes { get; set; }
        public string UpdateNotesFormat { get; set; }
    }
}