using System;

namespace TheLemmonWorkshopData.Models
{
    public class PhotoContent : IContentId, ICreatedAndLastUpdateOnAndBy, ITitleSummarySlug, IUpdateNotes
    {
        public string AltText { get; set; }
        public string PhotoCreatedBy { get; set; }
        public DateTime PhotoCreatedOn { get; set; }
        public string Camera { get; set; }
        public string Lens { get; set; }
        public string Aperture { get; set; }
        public string License { get; set; }
        public string ShutterSpeed { get; set; }
        public string BaseFileName { get; set; }
        public Guid Fingerprint { get; set; }
        public int Id { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string UpdateNotes { get; set; }
        public string UpdateNotesFormat { get; set; }
    }
}