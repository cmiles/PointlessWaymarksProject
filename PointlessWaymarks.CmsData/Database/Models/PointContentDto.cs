using System;
using System.Collections.Generic;

namespace PointlessWaymarks.CmsData.Database.Models
{
    public class PointContentDto : IUpdateNotes, IContentCommon
    {
        public double? Elevation { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<PointDetail> PointDetails { get; set; }
        public string? BodyContent { get; set; }
        public string? BodyContentFormat { get; set; }
        public Guid ContentId { get; set; }
        public DateTime ContentVersion { get; set; }
        public int Id { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public Guid? MainPicture { get; set; }
        public bool ShowInMainSiteFeed { get; set; }
        public string? Tags { get; set; }
        public string? Folder { get; set; }
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        public string? Title { get; set; }
        public string? UpdateNotes { get; set; }
        public string? UpdateNotesFormat { get; set; }
    }
}