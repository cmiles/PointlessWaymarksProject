using System;

namespace PointlessWaymarksCmsData.Database.Models
{
    public interface IContentCommon : IContentId, IMainImage, ITag, ITitleSummarySlugFolder,
        ICreatedAndLastUpdateOnAndBy, IShowInSiteFeed
    {
    }

    public class ContentCommonShell : IContentCommon
    {
        public Guid ContentId { get; set; }
        public DateTime ContentVersion { get; set; }
        public int Id { get; set; }
        public Guid? MainPicture { get; set; }
        public string Tags { get; set; }
        public string Folder { get; set; }
        public string Slug { get; set; }
        public string Summary { get; set; }
        public string Title { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public bool ShowInMainSiteFeed { get; set; }
    }
}