using HtmlTags;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class Footer
    {
        public static HtmlTag StandardFooterDiv()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var footerDiv = new HtmlTag("footer").AddClass("footer-container");

            footerDiv.Children.Add(
                new LinkTag(settings.SiteName, @$"//{settings.SiteUrl}").AddClass("footer-site-link-content"));

            var contentListDiv = new DivTag().AddClass("footer-content-lists-container");
            contentListDiv.Children.Add(
                new LinkTag("All Content", @$"//{settings.AllContentListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Posts", @$"//{settings.PostsListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Photos", @$"//{settings.PhotoListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Images", @$"//{settings.ImageListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Files", @$"//{settings.FileListUrl()}").AddClass("footer-content-list"));

            footerDiv.Children.Add(contentListDiv);

            if (!string.IsNullOrWhiteSpace(settings.SiteEmailTo))
                footerDiv.Children.Add(new HtmlTag("address").AddClass("footer-site-email")
                    .Text(settings.SiteEmailTo));

            return footerDiv;
        }
    }
}