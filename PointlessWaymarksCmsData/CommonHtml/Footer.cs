using HtmlTags;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class Footer
    {
        public static HtmlTag SiteTitleDiv()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var createdByDiv = new DivTag().AddClass("site-name-footer-container");
            createdByDiv.Children.Add(CommonHtml.HorizontalRule.StandardRule());

            createdByDiv.Children.Add(
                new LinkTag(settings.SiteName, @$"//{settings.SiteUrl}").AddClass("site-name-footer-content"));

            return createdByDiv;
        }
    }
}