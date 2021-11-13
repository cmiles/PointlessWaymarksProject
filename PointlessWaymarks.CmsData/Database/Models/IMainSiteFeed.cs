namespace PointlessWaymarks.CmsData.Database.Models
{
    public interface IMainSiteFeed
    {
        public DateTime FeedOn { get; set; }
        public bool IsDraft { get; set; }
        bool ShowInMainSiteFeed { get; }
    }
}