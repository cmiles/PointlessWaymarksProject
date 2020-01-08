using System.Linq;
using HtmlTags;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.PhotoHtml;

namespace PointlessWaymarksCmsData.PostHtml
{
    public partial class SinglePostDiv
    {
        public SinglePostDiv(PostContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsUtilities.ReadSettings().Result;
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.PostPageUrl(DbEntry);

            var db = Db.Context().Result;

            if (DbEntry.MainImage != null)
            {
                var dbImage = db.PhotoContents.SingleOrDefault(x => x.ContentId == DbEntry.MainImage.Value);

                if (dbImage != null) MainImage = new SinglePhotoPage(dbImage);
            }
        }

        public PostContent DbEntry { get; set; }

        public SinglePhotoPage MainImage { get; set; }

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }

        public HtmlTag TitleDiv()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var titleContainer = new HtmlTag("div").AddClass("post-title-link-container");

            var header = new HtmlTag("h1").AddClass("post-title-link-content");
            var linkToFullPost = new LinkTag(DbEntry.Title, settings.PostPageUrl(DbEntry));
            header.Children.Add(linkToFullPost);

            titleContainer.Children.Add(header);

            return titleContainer;
        }
    }
}