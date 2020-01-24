using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;

namespace PointlessWaymarksCmsData.PhotoListHtml
{
    public partial class PhotoListPage
    {
        public static HtmlTag PhotoTableTag()
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

                var pictureInfo =  new PictureSiteInformation(loopPhotos.ContentId);

                Tags.PictureImgTagWithSmallestDefaultSrc(pictureInfo.Pictures);
            }

            return photoListContainer;
        }
    }
}
