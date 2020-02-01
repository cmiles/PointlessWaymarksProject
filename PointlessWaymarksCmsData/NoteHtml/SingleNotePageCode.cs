﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.NoteHtml
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
            Title = $"Note - {CommonHtml.Tags.CreatedByAndUpdatedOnString(DbEntry)}";
        }

        public string Title { get; }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo =
                new FileInfo(
                    $"{Path.Combine(settings.LocalSiteNoteContentDirectory(DbEntry).FullName, DbEntry.Slug)}.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }

        public NoteContent DbEntry { get; }
        public string PageUrl { get; }
        public string SiteName { get; }
        public string SiteUrl { get; }

    }
}