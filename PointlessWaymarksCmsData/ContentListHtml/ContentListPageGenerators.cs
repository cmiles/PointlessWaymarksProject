using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.ContentListHtml
{
    public static class ContentListPageGenerators
    {
        public static void WriteAllContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                var fileContent = db.FileContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
                var photoContent = db.PhotoContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
                var imageContent = db.ImageContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
                var postContent = db.PostContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();

                return fileContent.Concat(photoContent).Concat(imageContent).Concat(postContent).OrderBy(x => x.Title)
                    .ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteAllContentListFile();

            WriteContentListHtml(ContentList, fileInfo, "All Content");
        }

        public static void WriteFileContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.FileContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteFileListFile();

            WriteContentListHtml(ContentList, fileInfo, "Files");
        }


        public static void WriteImageContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.ImageContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSiteImageListFile();

            WriteContentListHtml(ContentList, fileInfo, "Images");
        }

        public static void WritePostContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.PostContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePostListFile();

            WriteContentListHtml(ContentList, fileInfo, "Posts");
        }

        public static void WritePhotoContentListHtml()
        {
            List<IContentCommon> ContentList()
            {
                var db = Db.Context().Result;
                return db.PhotoContents.OrderBy(x => x.Title).Cast<IContentCommon>().ToList();
            }

            var fileInfo = UserSettingsSingleton.CurrentSettings().LocalSitePhotoListFile();

            WriteContentListHtml(ContentList, fileInfo, "Photos");
        }

        public static void WriteContentListHtml(Func<List<IContentCommon>> dbFunc, FileInfo fileInfo, string titleAdd)
        {
            var htmlModel = new ContentListPage {ContentFunction = dbFunc, ListTitle = titleAdd};

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
    }
}