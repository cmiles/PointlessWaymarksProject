using System.Collections.Generic;
using HtmlTags;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PhotoGalleryHtml
{
    public static class DailyPhotosPageParts
    {
        public static HtmlTag PhotoList(List<PictureSiteInformation> photos)
        {
            var containerDiv = new DivTag().AddClass("daily-photo-gallery-list-container");

            foreach (var loopPhotos in photos)
            {
                var photoContainer = new DivTag().AddClass("daily-photo-gallery-photo-container");
                photoContainer.Children.Add(loopPhotos.PictureFigureWithLinkToPicturePageTag());

                containerDiv.Children.Add(photoContainer);
            }

            return containerDiv;
        }
    }
}