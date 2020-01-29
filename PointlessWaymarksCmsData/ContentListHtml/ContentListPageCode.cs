using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.ContentListHtml
{
    public partial class ContentListPage
    {
        public Func<List<IContentCommon>> ContentFunction { get; set; }
        public string ListTitle { get; set; }

        public HtmlTag ContentTableTag()
        {
            var db = Db.Context().Result;

            var allContent = ContentFunction();

            var allContentContainer = new DivTag().AddClass("content-list-container");

            foreach (var loopPhotos in allContent)
            {
                var photoListPhotoEntryDiv = new DivTag().AddClass("content-list-item-container");
                photoListPhotoEntryDiv.Data("title", loopPhotos.Title);
                photoListPhotoEntryDiv.Data("tags", loopPhotos.Tags);
                photoListPhotoEntryDiv.Data("summary", loopPhotos.Summary);

                photoListPhotoEntryDiv.Children.Add(ContentCompact.FromContent(loopPhotos));

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
