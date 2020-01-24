using System.IO;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.ImageHtml
{
    public partial class SingleImagePage
    {
        public SingleImagePage(ImageContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.ImagePageUrl(DbEntry);

            PictureAsset = new PictureSiteInformation(DbEntry.ContentId);
        }

        public ImageContent DbEntry { get; }
        public string PageUrl { get; }
        public PictureSiteInformation PictureAsset { get; }
        public string SiteName { get; }
        public string SiteUrl { get; }


        public HtmlTag ImageSourceNotesDivTag()
        {
            if (string.IsNullOrWhiteSpace(DbEntry.ImageSourceNotes)) return HtmlTag.Empty();

            var sourceNotesContainer = new DivTag().AddClass("image-source-notes-container");
            var sourceNotes = new DivTag().AddClass("image-source-notes-content").Encoded(false)
                .Text(BracketCodeCommon.ProcessCodesAndMarkdownForSite($"Source: {DbEntry.ImageSourceNotes}"));
            sourceNotesContainer.Children.Add(sourceNotes);

            return sourceNotesContainer;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = settings.LocalSiteImageHtmlFile(DbEntry);

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}