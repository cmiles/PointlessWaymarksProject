using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PhotoListHtml
{
    public partial class PhotoListPage
    {
        public HtmlTag PhotoTableTag()
        {
            var db = Db.Context().Result;

            var allPhotosByName = db.PhotoContents.OrderBy(x => x.Title).ToList();

            var photoListContainer = new DivTag().AddClass("photo-list-list-container");

            foreach (var loopPhotos in allPhotosByName)
            {
                var photoListPhotoEntryDiv = new DivTag().AddClass("photo-list-list-item-container");
                photoListPhotoEntryDiv.Data("title", loopPhotos.Title);
                photoListPhotoEntryDiv.Data("tags", loopPhotos.Tags);
                photoListPhotoEntryDiv.Data("summary", loopPhotos.Summary);
                photoListPhotoEntryDiv.Data("alttext", loopPhotos.AltText);
                photoListPhotoEntryDiv.Data("contenttype", "photo");

                photoListPhotoEntryDiv.Children.Add(ContentCompact.FromContent(loopPhotos));

                photoListContainer.Children.Add(photoListPhotoEntryDiv);
            }

            return photoListContainer;
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
