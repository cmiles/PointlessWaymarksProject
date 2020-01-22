using HtmlTags;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.PostHtml
{
    public partial class SinglePostDiv
    {
        public SinglePostDiv(PostContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PostPageUrl(DbEntry);

            var db = Db.Context().Result;
        }

        public PostContent DbEntry { get; set; }

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }

        public HtmlTag TitleDiv()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var titleContainer = new HtmlTag("div").AddClass("post-title-link-container");

            var header = new HtmlTag("h1").AddClass("post-title-link-content");
            var linkToFullPost = new LinkTag(DbEntry.Title, settings.PostPageUrl(DbEntry));
            header.Children.Add(linkToFullPost);

            titleContainer.Children.Add(header);

            return titleContainer;
        }
    }
}