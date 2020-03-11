using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsData.Rss;

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
                var noteContent = db.NoteContents.Cast<IContentCommon>().ToList();

                return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).Concat(noteContent)
                    .OrderBy(x => x.Title).ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteAllContentListFile();

            WriteContentListHtml(ContentList, fileInfo, "All Content",
                UserSettingsSingleton.CurrentSettings().AllContentRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSiteAllContentRssFile(),
                "All Content");
        }

        public static void WriteContentListHtml(Func<List<IContentCommon>> dbFunc, FileInfo fileInfo, string titleAdd,
            string rssUrl)
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

        public static async void WriteContentListRss(List<IContentCommon> content, FileInfo fileInfo, string titleAdd)
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var items = new List<string>();

            foreach (var loopPosts in content)
            {
                var contentUrl = await settings.ContentUrl(loopPosts.ContentId);

                string itemDescription;

                if (loopPosts.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(loopPosts.MainPicture.Value);
                    itemDescription =
                        $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(loopPosts.Summary)}</p>" +
                        $"<p>Read more at <a href=\"https:{contentUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }
                else
                {
                    itemDescription = $"<p>{HttpUtility.HtmlEncode(loopPosts.Summary)}</p>" +
                                      $"<p>Read more at <a href=\"https:{contentUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                items.Add(RssStringBuilder.RssItemString(loopPosts.Title, $"https:{contentUrl}", itemDescription,
                    loopPosts.CreatedOn, loopPosts.ContentId.ToString()));
            }

            var localIndexFile = fileInfo;

            if (localIndexFile.Exists)
            {
                localIndexFile.Delete();
                localIndexFile.Refresh();
            }

            File.WriteAllText(localIndexFile.FullName,
                RssStringBuilder.RssFileString($"{UserSettingsSingleton.CurrentSettings().SiteName} - {titleAdd}",
                    string.Join(Environment.NewLine, items)), Encoding.UTF8);
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
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSiteFileRssFile(), "Files");
        }


        public static void WriteImageContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.ImageContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteImageListFile();

            WriteContentListHtml(ContentList, fileInfo, "Images",
                UserSettingsSingleton.CurrentSettings().ImageRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSiteImageRssFile(),
                "Images");
        }

        public static void WriteNoteContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.NoteContents.ToList().OrderByDescending(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteNoteListFile();

            WriteContentListHtml(ContentList, fileInfo, "Notes", UserSettingsSingleton.CurrentSettings().NoteRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSiteNoteRssFile(), "Notes");
        }

        public static void WritePhotoContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.PhotoContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePhotoListFile();

            WriteContentListHtml(ContentList, fileInfo, "Photos",
                UserSettingsSingleton.CurrentSettings().PhotoRssUrl());
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSitePhotoRssFile(),
                "Photos");
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
            WriteContentListRss(ContentList(), UserSettingsSingleton.CurrentSettings().LocalSitePostRssFile(), "Posts");
        }
    }
}