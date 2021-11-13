﻿using HtmlTags;

namespace PointlessWaymarks.CmsData.CommonHtml
{
    public static class Footer
    {
        public static async Task<HtmlTag> StandardFooterDiv()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var footerDiv = new HtmlTag("footer").AddClass("footer-container");

            footerDiv.Children.Add(await Tags.CoreLinksDiv().ConfigureAwait(false));

            if (!string.IsNullOrWhiteSpace(settings.SiteEmailTo))
            {
                var emailAddress = new HtmlTag("address").AddClass("footer-site-email").Text(settings.SiteEmailTo);

                var emailListDiv = new DivTag().AddClass("footer-content-lists-container");
                emailListDiv.Children.Add(emailAddress);

                footerDiv.Children.Add(emailListDiv);
            }

            footerDiv.Children.Add(
                new LinkTag(settings.SiteName, @$"//{settings.SiteUrl}").AddClass("footer-site-link-content"));

            return footerDiv;
        }
    }
}