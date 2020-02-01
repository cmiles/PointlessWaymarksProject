using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.NoteHtml;
using PointlessWaymarksCmsData.PostHtml;

namespace PointlessWaymarksCmsData.ContentListHtml
{
    public static class ContentListPageGenerators
    {
        public static void WriteAllContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                var fileContent = db.FileContents.Cast<IContentCommon>().ToList();
                var photoContent = db.PhotoContents.Cast<IContentCommon>().ToList();
                var imageContent = db.ImageContents.Cast<IContentCommon>().ToList();
                var postContent = db.PostContents.Cast<IContentCommon>().ToList();
                var noteContent = db.NoteContents.ToList().Select(x => x.NoteToCommonContent()).Cast<IContentCommon>()
                    .ToList();

                return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(noteContent)
                    .OrderBy(x => x.Title).ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteAllContentListFile();

            WriteContentListHtml(ContentList, fileInfo, "All Content", UserSettingsSingleton.CurrentSettings().AllContentRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSiteAllContentRssFile(),
                "All Content", UserSettingsSingleton.CurrentSettings().AllContentRssUrl());
        }

        public static void WriteFileContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.FileContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteFileListFile();

            WriteContentListHtml(ContentList, fileInfo, "Files", UserSettingsSingleton.CurrentSettings().FileRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSiteFileRssFile(),
                "Files", UserSettingsSingleton.CurrentSettings().FileRssUrl());
        }


        public static void WriteImageContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.ImageContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteImageListFile();

            WriteContentListHtml(ContentList, fileInfo, "Images", UserSettingsSingleton.CurrentSettings().ImageRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSiteImageRssFile(),
                "Images", UserSettingsSingleton.CurrentSettings().ImageRssUrl());
        }

        public static void WritePostContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.PostContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePostListFile();

            WriteContentListHtml(ContentList, fileInfo, "Posts", UserSettingsSingleton.CurrentSettings().PostsRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSitePostRssFile(),
                "Posts", UserSettingsSingleton.CurrentSettings().PostsRssUrl());
        }

        public static void WritePhotoContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.PhotoContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePhotoListFile();

            WriteContentListHtml(ContentList, fileInfo, "Photos", UserSettingsSingleton.CurrentSettings().PhotoRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSitePhotoRssFile(),
                "Photos", UserSettingsSingleton.CurrentSettings().PhotoRssUrl());
        }

        public static ContentCommon NoteToCommonContent(this NoteContent toTransform)
        {
            return new ContentCommon
            {
                ContentId = toTransform.ContentId,
                CreatedBy = toTransform.CreatedBy,
                CreatedOn = toTransform.CreatedOn,
                Folder = toTransform.Folder,
                Id = toTransform.Id,
                LastUpdatedBy = toTransform.LastUpdatedBy,
                LastUpdatedOn = toTransform.LastUpdatedOn,
                MainPicture = null,
                Slug = toTransform.Slug,
                Summary = toTransform.Summary,
                Tags = toTransform.Tags,
                Title = NoteParts.TitleString(toTransform)
            };
        }

        public static void WriteNoteContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.NoteContents.ToList().Select(x => x.NoteToCommonContent()).Cast<IContentCommon>()
                    .OrderByDescending(x => x.Title).ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteNoteListFile();

            WriteContentListHtml(ContentList, fileInfo, "Notes", UserSettingsSingleton.CurrentSettings().NoteRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSiteNoteRssFile(),
                "Notes", UserSettingsSingleton.CurrentSettings().NoteRssUrl());
        }

        public static void WriteContentListHtml(Func<List<IContentCommon>> dbFunc, FileInfo fileInfo, string titleAdd, string rssUrl)
        {
            var htmlModel = new ContentListPage(rssUrl) {ContentFunction = dbFunc, ListTitle = titleAdd};

            var htmlTransform = htmlModel.TransformText();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(htmlTransform);

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            if (fileInfo.Exists)
            {
                fileInfo.Delete();
                fileInfo.Refresh();
            }

            File.WriteAllText(fileInfo.FullName, htmlString);
        }

        public static async void WriteContentListRss(List<IContentCommon> content, FileInfo fileInfo, string titleAdd,
            string rssFileUrl)
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var feed = new SyndicationFeed(settings.SiteName, $"{settings.SiteSummary} - {titleAdd}",
                new Uri($"https://{settings.SiteUrl}"), $"https:{rssFileUrl}", DateTime.Now);
            feed.Copyright = new TextSyndicationContent($"{DateTime.Now.Year} {settings.SiteAuthors}");

            var items = new List<SyndicationItem>();

            foreach (var loopPosts in content)
            {
                var contentUrl = await settings.ContentUrl(loopPosts.ContentId);
                items.Add(new SyndicationItem(loopPosts.Title, loopPosts.Summary, new Uri($"https:{contentUrl}"),
                    loopPosts.Slug, loopPosts.CreatedOn));
            }

            feed.Items = items;

            var xmlSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = true,
                Indent = true
            };

            using var stream = new MemoryStream();

            var localIndexFile = fileInfo;

            if (localIndexFile.Exists)
            {
                localIndexFile.Delete();
                localIndexFile.Refresh();
            }

            using (var xmlWriter = XmlWriter.Create(localIndexFile.FullName, xmlSettings))
            {
                var rssFormatter = new Rss20FeedFormatter(feed, false);
                rssFormatter.WriteTo(xmlWriter);
                xmlWriter.Flush();
            }
        }
    }
}