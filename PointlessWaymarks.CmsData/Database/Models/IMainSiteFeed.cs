using System;

namespace PointlessWaymarks.CmsData.Database.Models
{
    public interface IMainSiteFeed
    {
        public bool IsDraft { get; set; }
        public DateTime FeedOn { get; set; }
        bool ShowInMainSiteFeed { get; }
    }
}