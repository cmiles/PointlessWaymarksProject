using System;
using System.Collections.Generic;
using System.IO;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.ContentListHtml
{
    public partial class ContentListPage
    {
        public ContentListPage(string rssUrl)
        {
            RssUrl = rssUrl;
        }

        public Func<List<IContentCommon>> ContentFunction { get; set; }
        public string ListTitle { get; set; }

        public string RssUrl { get; set; }

        public HtmlTag ContentTableTag()
        {
            var db = Db.Context().Result;

            var allContent = ContentFunction();

            var allContentContainer = new DivTag().AddClass("content-list-container");

            foreach (var loopContent in allContent)
            {
                var photoListPhotoEntryDiv = new DivTag().AddClass("content-list-item-container");
                photoListPhotoEntryDiv.Data("title", loopContent.Title);
                photoListPhotoEntryDiv.Data("tags", loopContent.Tags);
                photoListPhotoEntryDiv.Data("summary", loopContent.Summary);

                switch (loopContent)
                {
                    case FileContent x:
                        photoListPhotoEntryDiv.Data("contenttype", "files");
                        break;
                    case ImageContent x:
                        photoListPhotoEntryDiv.Data("contenttype", "picture");
                        break;
                    case NoteContent x:
                        photoListPhotoEntryDiv.Data("contenttype", "post");
                        break;
                    case PhotoContent x:
                        photoListPhotoEntryDiv.Data("contenttype", "picture");
                        break;
                    case PostContent x:
                        photoListPhotoEntryDiv.Data("contenttype", "post");
                        break;
                    case LinkStream x:
                        photoListPhotoEntryDiv.Data("contenttype", "link");
                        break;
                    default:
                        photoListPhotoEntryDiv.Data("contenttype", "other");
                        break;
                }


                photoListPhotoEntryDiv.Children.Add(ContentCompact.FromContent(loopContent));

                allContentContainer.Children.Add(photoListPhotoEntryDiv);
            }

            return allContentContainer;
        }

        public void WriteLocalHtml()
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo = settings.LocalSitePhotoListFile();

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }
    }
}