using System.Collections.Generic;
using System.IO;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.FileHtml
{
    public partial class SingleFilePage
    {
        public SingleFilePage(FileContent dbEntry)
        {
            DbEntry = dbEntry;

            var settings = UserSettingsUtilities.ReadSettings().Result;
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            PageUrl = settings.FilePageUrl(DbEntry);

            var db = Db.Context().Result;
        }

        public FileContent DbEntry { get; set; }

        public List<FileContent> LaterFiles { get; set; }

        public string PageUrl { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }

        public HtmlTag TitleDiv()
        {
            var titleContainer = new HtmlTag("div").AddClass("post-title-container");
            titleContainer.Children.Add(new HtmlTag("h1").AddClass("post-title-content").Text(DbEntry.Title));
            return titleContainer;
        }

        public HtmlTag UpdateDiv()
        {
            if (string.IsNullOrWhiteSpace(DbEntry.UpdateNotes)) return HtmlTag.Empty();

            var updateNotesDiv = new DivTag().AddClass("update-notes-container");

            updateNotesDiv.Children.Add(HorizontalRule.StandardRule());

            var updateNotesContentContainer = new DivTag().AddClass("update-notes-content");

            var updateNotesHtml = ContentProcessor.ContentHtml(DbEntry.UpdateNotesFormat, DbEntry.UpdateNotes);

            if (updateNotesHtml.success) updateNotesContentContainer.Encoded(false).Text(updateNotesHtml.output);

            updateNotesDiv.Children.Add(updateNotesContentContainer);

            return updateNotesDiv;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;

            var htmlString = TransformText();

            var htmlFileInfo =
                new FileInfo(
                    $"{Path.Combine(settings.LocalSiteFileContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}