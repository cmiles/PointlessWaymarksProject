using System;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.Pictures
{
    public class PictureSiteInformation
    {
        public PictureSiteInformation(Guid toLoad)
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            Pictures = PictureAssetProcessing.ProcessPictureDirectory(toLoad);
            PageUrl = settings.PicturePageUrl(toLoad);
        }

        public string PageUrl { get; set; }

        public PictureAsset Pictures { get; set; }

        public HtmlTag ImageFigureTag(ImageContent dbEntry)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            figureTag.Children.Add(Tags.PictureImgTag(Pictures));
            figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry));
            return figureTag;
        }

        public HtmlTag ImageFigureWithLinkToPageTag(ImageContent dbEntry)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            var linkTag = new LinkTag(string.Empty, PageUrl);
            linkTag.Children.Add(Tags.PictureImgTag(Pictures));
            figureTag.Children.Add(linkTag);
            figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry));
            return figureTag;
        }

        public HtmlTag LocalDisplayPhotoImageTag()
        {
            var imageTag = new HtmlTag("img").AddClass("single-photo")
                .Attr("src", $"file://{Pictures.DisplayPicture.File.FullName}")
                .Attr("height", Pictures.DisplayPicture.Height).Attr("width", Pictures.DisplayPicture.Width)
                .Attr("loading", "lazy");

            if (!string.IsNullOrWhiteSpace(Pictures.DisplayPicture.AltText))
                imageTag.Attr("alt", Pictures.DisplayPicture.AltText);

            return imageTag;
        }

        private HtmlTag LocalImageFigureTag(ImageContent dbEntry)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            figureTag.Children.Add(LocalDisplayPhotoImageTag());
            figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry));
            return figureTag;
        }

        private HtmlTag LocalPhotoFigureTag(PhotoContent dbEntry)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            figureTag.Children.Add(LocalDisplayPhotoImageTag());
            figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry));
            return figureTag;
        }

        public HtmlTag LocalPictureFigureTag()
        {
            switch (Pictures.DbEntry)
            {
                case PhotoContent p:
                    return LocalPhotoFigureTag(p);
                case ImageContent i:
                    return LocalImageFigureTag(i);
                default:
                    throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry));
            }
        }

        public HtmlTag PhotoFigureTag(PhotoContent dbEntry)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            figureTag.Children.Add(Tags.PictureImgTag(Pictures));
            figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry));
            return figureTag;
        }

        public HtmlTag PhotoFigureWithLinkToPageTag(PhotoContent dbEntry)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            var linkTag = new LinkTag(string.Empty, PageUrl);
            linkTag.Children.Add(Tags.PictureImgTag(Pictures));
            figureTag.Children.Add(linkTag);
            figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry));
            return figureTag;
        }

        public HtmlTag PictureFigureTag()
        {
            switch (Pictures.DbEntry)
            {
                case PhotoContent p:
                    return PhotoFigureTag(p);
                case ImageContent i:
                    return ImageFigureTag(i);
                default:
                    throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry));
            }
        }

        public HtmlTag PictureFigureWithLinkToPicturePageTag()
        {
            switch (Pictures.DbEntry)
            {
                case PhotoContent p:
                    return PhotoFigureWithLinkToPageTag(p);
                case ImageContent i:
                    return ImageFigureWithLinkToPageTag(i);
                default:
                    throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry));
            }
        }
    }
}