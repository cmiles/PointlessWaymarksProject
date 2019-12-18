using System;

namespace TheLemmonWorkshopData.Models
{
    public class PostContent : IContentId, ICreatedAndLastUpdateOnAndBy, ITitleSummarySlug, IUpdateNotes, IMainImage,
        IBodyContent
    {
        public string BodyContent { get; set; }
        public string BodyContentFormat { get; set; }
        public Guid Fingerprint { get; set; }
        public int Id { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }

        public string MainImage { get; set; }

        public string MainImageFormat { get; set; }
        public string Slug { get; set; }

        public string Summary { get; set; }
        public string Title { get; set; }
        public string UpdateNotes { get; set; }
        public string UpdateNotesFormat { get; set; }
    }
}