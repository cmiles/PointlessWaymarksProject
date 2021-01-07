namespace PointlessWaymarks.CmsData.Database.Models
{
    public interface ITitleSummarySlugFolder
    {
        public string Folder { get; }
        public string Slug { get; }
        public string Summary { get; }
        public string Title { get; }
    }
}