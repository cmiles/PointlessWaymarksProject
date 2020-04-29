using System.Linq;
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
            contentListDiv.Children.Add(new DivTag().AddClass("footer-content-list").Text("List/Search:"));
            contentListDiv.Children.Add(
                new LinkTag("All Content", @$"{settings.AllContentListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Posts", @$"{settings.PostsListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Photos", @$"{settings.PhotoListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Images", @$"{settings.ImageListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Files", @$"{settings.FileListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Notes", @$"{settings.NoteListUrl()}").AddClass("footer-content-list"));
            contentListDiv.Children.Add(
                new LinkTag("Links", @$"{settings.LinkListUrl()}").AddClass("footer-content-list"));

            footerDiv.Children.Add(contentListDiv);

            if (!string.IsNullOrWhiteSpace(settings.SiteEmailTo))
            {
                var emailAddress = new HtmlTag("address").AddClass("footer-site-email").Text(settings.SiteEmailTo);

                var emailListDiv = new DivTag().AddClass("footer-content-lists-container");
                emailListDiv.Children.Add(emailAddress);

                footerDiv.Children.Add(emailListDiv);
            }

            var db = Db.Context().Result;

            var staticLinksDiv = new DivTag().AddClass("footer-content-lists-container");

            var cameraRollLink = new LinkTag("Camera Roll",
                UserSettingsSingleton.CurrentSettings().CameraRollPhotoGalleryUrl(), "footer-camera-roll-link");

            staticLinksDiv.Children.Add(cameraRollLink);

            var possibleAbout = db.PostContents.Where(x => x.Title.ToLower() == "about").ToList();

            if (possibleAbout.Count > 0)
            {
                var aboutToUse = possibleAbout.OrderByDescending(x => x.CreatedOn).First();

                var aboutLink = new LinkTag("About", settings.PostPageUrl(aboutToUse)).AddClass("footer-site-about");

                staticLinksDiv.Children.Add(aboutLink);

                footerDiv.Children.Add(staticLinksDiv);
            }

            return footerDiv;
        }
    }
}