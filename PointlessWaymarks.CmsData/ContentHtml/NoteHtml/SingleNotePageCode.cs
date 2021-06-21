using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.NoteHtml
{
    public partial class SingleNotePage
    {
        public SingleNotePage(NoteContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.NotePageUrl(DbEntry);
            Title = DbEntry.Title;
            LangAttribute = settings.SiteLangAttribute;
            DirAttribute = settings.SiteDirectionAttribute;

            var (previousContent, laterContent) = Tags.MainFeedPreviousAndLaterContent(3, DbEntry.CreatedOn);
            PreviousPosts = previousContent;
            LaterPosts = laterContent;
        }

        public NoteContent DbEntry { get; }

        public string DirAttribute { get; set; }
        public DateTime? GenerationVersion { get; set; }

        public string LangAttribute { get; set; }
        public List<IContentCommon> LaterPosts { get; set; }
        public string PageUrl { get; }
        public List<IContentCommon> PreviousPosts { get; set; }
        public string SiteName { get; }
        public string SiteUrl { get; }
        public string Title { get; }

        public async Task WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = settings.LocalSiteNoteHtmlFile(DbEntry);

            if (htmlFileInfo == null)
            {
                var toThrow =
                    new Exception("The Note DbEntry did not have valid information to determine a file for the html");
                toThrow.Data.Add("Note DbEntry", ObjectDumper.Dump(DbEntry));
                throw toThrow;
            }

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
        }
    }
}