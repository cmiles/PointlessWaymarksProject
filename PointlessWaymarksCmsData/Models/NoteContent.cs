using System;
using System.ComponentModel.DataAnnotations.Schema;
using PointlessWaymarksCmsData.NoteHtml;

namespace PointlessWaymarksCmsData.Models
{
    public class NoteContent : IBodyContent, IContentCommon
    {
        public string BodyContent { get; set; }
        public string BodyContentFormat { get; set; }
        public string Folder { get; set; }
        public string Slug { get; set; }

        public string Summary { get; set; }
        public int Id { get; set; }
        public Guid ContentId { get; set; }
        public DateTime ContentVersion { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public bool ShowInMainSiteFeed { get; set; }
        public string Tags { get; set; }

        [NotMapped] public string Title => $"Notes - {NoteParts.NoteCreatedByAndUpdatedOnString(this)}";

        [NotMapped] public Guid? MainPicture => null;
    }
}