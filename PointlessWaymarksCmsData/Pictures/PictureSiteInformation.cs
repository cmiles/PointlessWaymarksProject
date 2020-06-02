using System;
using System.IO;
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

        public HtmlTag ImageFigureTag(ImageContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            return figureTag;
        }

        public HtmlTag ImageFigureWithCaptionAndLinkToPageTag(ImageContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            var linkTag = new LinkTag(string.Empty, PageUrl);
            linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            figureTag.Children.Add(linkTag);
            figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry));
            return figureTag;
        }

        public HtmlTag ImageFigureWithCaptionTag(ImageContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry));
            return figureTag;
        }

        public HtmlTag ImageFigureWithLinkToPageTag(ImageContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            var linkTag = new LinkTag(string.Empty, PageUrl);
            linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            figureTag.Children.Add(linkTag);
            return figureTag;
        }

        public HtmlTag ImageFigureWithTitleCaptionTag(ImageContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-image-container");
            figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry, true));
            return figureTag;
        }


        public HtmlTag LocalDisplayPhotoImageTag()
        {
            var imageTag = new HtmlTag("img").AddClass("single-photo")
                .Attr("src",
                    $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(Pictures.DisplayPicture.File.FullName))}")
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

        private HtmlTag EmailImageTableTag(ImageContent dbEntry)
        {
            var tableContainer = new TableTag();
            tableContainer.Style("margin", "20px").Style("text-align", "center");
            var pictureRow = tableContainer.AddBodyRow();
            var pictureCell = pictureRow.Cell();
            pictureCell.Children.Add(Tags.PictureEmailImgTag(Pictures, true));

            var captionRow = tableContainer.AddBodyRow();
            var captionCell = captionRow.Cell(Tags.ImageCaptionText(dbEntry, false));
            captionCell.Style("opacity", ".5");

            return tableContainer;
        }

        private HtmlTag LocalPhotoFigureTag(PhotoContent dbEntry)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            figureTag.Children.Add(LocalDisplayPhotoImageTag());
            figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry));
            return figureTag;
        }

        private HtmlTag EmailPhotoTableTag(PhotoContent dbEntry)
        {
            var tableContainer = new TableTag();
            tableContainer.Style("margin", "20px").Style("text-align", "center");
            var pictureRow = tableContainer.AddBodyRow();
            var pictureCell = pictureRow.Cell();
            pictureCell.Children.Add(Tags.PictureEmailImgTag(Pictures, true));

            var captionRow = tableContainer.AddBodyRow();
            var captionCell = captionRow.Cell(Tags.PhotoCaptionText(dbEntry, false));
            captionCell.Style("opacity", ".5");

            return tableContainer;
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

        public HtmlTag EmailPictureTableTag()
        {
            switch (Pictures.DbEntry)
            {
                case PhotoContent p:
                    return EmailPhotoTableTag(p);
                case ImageContent i:
                    return EmailImageTableTag(i);
                default:
                    throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry));
            }
        }

        public HtmlTag PhotoFigureTag(PhotoContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            return figureTag;
        }

        public HtmlTag PhotoFigureWithCaptionAndLinkToPageTag(PhotoContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            var linkTag = new LinkTag(string.Empty, PageUrl);
            linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            figureTag.Children.Add(linkTag);
            figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry));
            return figureTag;
        }

        public HtmlTag PhotoFigureWithCaptionTag(PhotoContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry));
            return figureTag;
        }

        public HtmlTag PhotoFigureWithLinkToPageTag(PhotoContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            var linkTag = new LinkTag(string.Empty, PageUrl);
            linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            figureTag.Children.Add(linkTag);
            return figureTag;
        }

        public HtmlTag PhotoFigureWithTitleCaptionTag(PhotoContent dbEntry, string sizes)
        {
            var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
            figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
            figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry, true));
            return figureTag;
        }

        public HtmlTag PictureFigureTag(string sizes)
        {
            switch (Pictures.DbEntry)
            {
                case PhotoContent p:
                    return PhotoFigureTag(p, sizes);
                case ImageContent i:
                    return ImageFigureTag(i, sizes);
                default:
                    throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry));
            }
        }

        public HtmlTag PictureFigureWithCaptionAndLinkToPicturePageTag(string sizes)
        {
            switch (Pictures.DbEntry)
            {
                case PhotoContent p:
                    return PhotoFigureWithCaptionAndLinkToPageTag(p, sizes);
                case ImageContent i:
                    return ImageFigureWithCaptionAndLinkToPageTag(i, sizes);
                default:
                    throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry));
            }
        }


        public HtmlTag PictureFigureWithCaptionTag(string sizes)
        {
            switch (Pictures.DbEntry)
            {
                case PhotoContent p:
                    return PhotoFigureWithCaptionTag(p, sizes);
                case ImageContent i:
                    return ImageFigureWithCaptionTag(i, sizes);
                default:
                    throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry));
            }
        }

        public HtmlTag PictureFigureWithLinkToPicturePageTag(string sizes)
        {
            switch (Pictures.DbEntry)
            {
                case PhotoContent p:
                    return PhotoFigureWithLinkToPageTag(p, sizes);
                case ImageContent i:
                    return ImageFigureWithLinkToPageTag(i, sizes);
                default:
                    throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry));
            }
        }

        public HtmlTag PictureFigureWithTitleCaptionTag(string sizes)
        {
            switch (Pictures.DbEntry)
            {
                case PhotoContent p:
                    return PhotoFigureWithTitleCaptionTag(p, sizes);
                case ImageContent i:
                    return ImageFigureWithTitleCaptionTag(i, sizes);
                default:
                    throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry));
            }
        }
    }
}