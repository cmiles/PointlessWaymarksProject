using System;

namespace PointlessWaymarksCmsData.Models
{
    public class PhotoContent : IContentId, ICreatedAndLastUpdateOnAndBy, ITitleSummarySlugFolder, IUpdateNotes, ITag
    {
        public string AltText { get; set; }
        public string Aperture { get; set; }
        public string OriginalFileName { get; set; }
        public string CameraMake { get; set; }
        public string CameraModel { get; set; }
        public Guid ContentId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string FocalLength { get; set; }
        public string Folder { get; set; }
        public int Id { get; set; }
        public int? Iso { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string Lens { get; set; }
        public string License { get; set; }
        public string PhotoCreatedBy { get; set; }
        public DateTime PhotoCreatedOn { get; set; }
        public string ShutterSpeed { get; set; }
        public string Slug { get; set; }
        public string Summary { get; set; }
        public string Tags { get; set; }
        public string Title { get; set; }
        public string UpdateNotes { get; set; }
        public string UpdateNotesFormat { get; set; }
    }
}