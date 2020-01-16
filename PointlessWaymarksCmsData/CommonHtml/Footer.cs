using HtmlTags;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class Footer
    {
        public static HtmlTag StandardFooterDiv()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var footerDiv = new HtmlTag("footer").AddClass("site-name-footer-container");

            footerDiv.Children.Add(
                new LinkTag(settings.SiteName, @$"//{settings.SiteUrl}").AddClass("site-name-footer-content"));
            if (!string.IsNullOrWhiteSpace(settings.SiteEmailTo))
                footerDiv.Children.Add(new HtmlTag("address").AddClass("site-name-footer-email")
                    .Text(settings.SiteEmailTo));

            return footerDiv;
        }
    }
}