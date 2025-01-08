using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;

// ReSharper disable MustUseReturnValue
// A number of methods in HtmlTags show this warning, and I am not convinced it
// is worth heeding?

namespace PointlessWaymarks.CmsData.CommonHtml;

public class PictureSiteInformation
{
    public PictureSiteInformation(Guid toLoad)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        Pictures = PictureAssetProcessing.ProcessPictureDirectory(toLoad);
        PageUrl = settings.PicturePageUrl(toLoad);
    }

    public string? PageUrl { get; }

    public PictureAsset? Pictures { get; }

    private HtmlTag EmailImageTableTag(ImageContent dbEntry)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var tableContainer = new TableTag();
        tableContainer.Style("margin", "20px").Style("text-align", "center");
        var pictureRow = tableContainer.AddBodyRow();
        var pictureCell = pictureRow.Cell();
        pictureCell.Children.Add(Tags.PictureEmailImgTag(Pictures, true));

        var captionRow = tableContainer.AddBodyRow();
        var captionCell = captionRow.Cell(Tags.ImageCaptionText(dbEntry));
        captionCell.Style("opacity", ".5");

        return tableContainer;
    }

    private HtmlTag EmailPhotoTableTag(PhotoContent dbEntry, bool includePhotoDetails)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var emailCenterTable = new TableTag();
        emailCenterTable.Attr("width", "94%");
        emailCenterTable.Attr("margin", "20");
        emailCenterTable.Attr("border", "0");
        emailCenterTable.Attr("cellspacing", "0");
        emailCenterTable.Attr("cellpadding", "0");

        var topMarginRow = emailCenterTable.AddBodyRow();
        topMarginRow.Attr("height", "10");
        var topMarginCell = topMarginRow.Cell();
        topMarginCell.Text("&nbsp;").Encoded(false);

        var emailImageRow = emailCenterTable.AddBodyRow();

        var emailImageCenterLeftCell = emailImageRow.Cell();
        emailImageCenterLeftCell.Attr("max-width", "1%");
        emailImageCenterLeftCell.Attr("align", "center");
        emailImageCenterLeftCell.Attr("valign", "top");
        emailImageCenterLeftCell.Text("&nbsp;").Encoded(false);

        var emailCenterContentCell = emailImageRow.Cell();
        emailCenterContentCell.Attr("width", "100%");
        emailCenterContentCell.Attr("align", "center");
        emailCenterContentCell.Attr("valign", "top");

        emailCenterContentCell.Children.Add(Tags.PictureEmailImgTag(Pictures, true));

        var emailCenterRightCell = emailImageRow.Cell();
        emailCenterRightCell.Attr("max-width", "1%");
        emailCenterRightCell.Attr("align", "center");
        emailCenterRightCell.Attr("valign", "top");
        emailCenterRightCell.Text("&nbsp;").Encoded(false);

        var captionImageRow = emailCenterTable.AddBodyRow();

        var captionImageCenterLeftCell = captionImageRow.Cell();
        captionImageCenterLeftCell.Attr("max-width", "1%");
        captionImageCenterLeftCell.Attr("align", "center");
        captionImageCenterLeftCell.Attr("valign", "top");
        captionImageCenterLeftCell.Text("&nbsp;").Encoded(false);

        var captionCenterContentCell = captionImageRow.Cell();
        captionCenterContentCell.Attr("width", "100%");
        captionCenterContentCell.Attr("align", "center");
        captionCenterContentCell.Attr("valign", "top");

        captionCenterContentCell.Text(Tags.PhotoCaptionText(dbEntry, includePhotoDetails: includePhotoDetails));

        var captionCenterRightCell = captionImageRow.Cell();
        captionCenterRightCell.Attr("max-width", "1%");
        captionCenterRightCell.Attr("align", "center");
        captionCenterRightCell.Attr("valign", "top");
        captionCenterRightCell.Text("&nbsp;").Encoded(false);

        var bottomMarginRow = emailCenterTable.AddBodyRow();
        bottomMarginRow.Attr("height", "10");
        var bottomMarginCell = bottomMarginRow.Cell();
        bottomMarginCell.Text("&nbsp;").Encoded(false);

        return emailCenterTable;
    }

    public HtmlTag EmailPictureTableTag(bool ifPhotoIncludeDetails = false)
    {
        if (Pictures == null) return HtmlTag.Empty();

        return Pictures.DbEntry switch
        {
            PhotoContent p => EmailPhotoTableTag(p, ifPhotoIncludeDetails),
            ImageContent i => EmailImageTableTag(i),
            _ => throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry))
        };
    }

    public HtmlTag ImageFigureTag(string sizes)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-image-container");
        figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        return figureTag;
    }

    public HtmlTag ImageFigureWithCaptionAndLinkTag(ImageContent dbEntry, string sizes, string linkUrl)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-image-container");
        var linkTag = new LinkTag(string.Empty, linkUrl);
        linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(linkTag);
        figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry));
        return figureTag;
    }

    public HtmlTag ImageFigureWithCaptionAndLinkToPageTag(ImageContent dbEntry, string sizes)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-image-container");
        var linkTag = new LinkTag(string.Empty, PageUrl);
        linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(linkTag);
        figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry));
        return figureTag;
    }

    public HtmlTag ImageFigureWithCaptionTag(ImageContent dbEntry, string sizes)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-image-container");
        figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry));
        return figureTag;
    }

    public HtmlTag ImageFigureWithLinkToPageTag(string sizes)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-image-container");
        var linkTag = new LinkTag(string.Empty, PageUrl);
        linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(linkTag);
        return figureTag;
    }

    public HtmlTag ImageFigureWithTitleCaptionTag(ImageContent dbEntry, string sizes)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-image-container");
        figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(Tags.ImageFigCaptionTag(dbEntry, true));
        return figureTag;
    }


    public HtmlTag LocalDisplayPhotoImageTag()
    {
        if (Pictures?.DisplayPicture?.File == null) return HtmlTag.Empty();

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

    private HtmlTag LocalPhotoFigureTag(PhotoContent dbEntry, bool includePhotoDetails)
    {
        var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
        figureTag.Children.Add(LocalDisplayPhotoImageTag());
        figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry, includePhotoDetails: includePhotoDetails));
        return figureTag;
    }

    public HtmlTag LocalPictureFigureTag(bool ifPhotoIncludeDetails = false)
    {
        if (Pictures == null) return HtmlTag.Empty();

        return Pictures.DbEntry switch
        {
            PhotoContent p => LocalPhotoFigureTag(p, ifPhotoIncludeDetails),
            ImageContent i => LocalImageFigureTag(i),
            _ => throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry))
        };
    }

    public HtmlTag PhotoFigureTag(string sizes)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
        figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        return figureTag;
    }

    public HtmlTag PhotoFigureWithCaptionAndLinkTag(PhotoContent dbEntry, string sizes, string linkUrl,
        bool includePhotoDetails)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
        var linkTag = new LinkTag(string.Empty, linkUrl);
        linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(linkTag);
        figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry, includePhotoDetails: includePhotoDetails));
        return figureTag;
    }

    public HtmlTag PhotoFigureWithCaptionAndLinkToPageTag(PhotoContent dbEntry, string sizes, bool includePhotoDetails)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
        var linkTag = new LinkTag(string.Empty, PageUrl);
        linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(linkTag);
        figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry, includePhotoDetails: includePhotoDetails));
        return figureTag;
    }

    public HtmlTag PhotoFigureWithCaptionTag(PhotoContent dbEntry, string sizes, bool includePhotoDetails)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
        figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry, includePhotoDetails: includePhotoDetails));
        return figureTag;
    }

    public HtmlTag PhotoFigureWithLinkToPageTag(string sizes)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
        var linkTag = new LinkTag(string.Empty, PageUrl);
        linkTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(linkTag);
        return figureTag;
    }

    public HtmlTag PhotoFigureWithTitleCaptionTag(PhotoContent dbEntry, string sizes, bool includePhotoDetails)
    {
        if (Pictures == null) return HtmlTag.Empty();

        var figureTag = new HtmlTag("figure").AddClass("single-photo-container");
        figureTag.Children.Add(Tags.PictureImgTag(Pictures, sizes, true));
        figureTag.Children.Add(Tags.PhotoFigCaptionTag(dbEntry, includePhotoDetails: includePhotoDetails));
        return figureTag;
    }

    public HtmlTag PictureFigureTag(string sizes)
    {
        if (Pictures == null) return HtmlTag.Empty();

        return Pictures.DbEntry switch
        {
            PhotoContent => PhotoFigureTag(sizes),
            ImageContent => ImageFigureTag(sizes),
            _ => throw new Exception(
                $"{nameof(Pictures.DbEntry)} is not a recognized picture type for {nameof(PictureFigureWithCaptionAndLinkTag)}")
        };
    }

    public HtmlTag PictureFigureWithCaptionAndLinkTag(string sizes, string linkUrl, bool ifPhotoIncludeDetails = false)
    {
        if (Pictures == null) return HtmlTag.Empty();

        return Pictures.DbEntry switch
        {
            PhotoContent p => PhotoFigureWithCaptionAndLinkTag(p, sizes, linkUrl, ifPhotoIncludeDetails),
            ImageContent i => ImageFigureWithCaptionAndLinkTag(i, sizes, linkUrl),
            _ => throw new Exception(
                $"{nameof(Pictures.DbEntry)} is not a recognized picture type for {nameof(PictureFigureWithCaptionAndLinkTag)}")
        };
    }

    public HtmlTag PictureFigureWithCaptionAndLinkToPicturePageTag(string sizes, bool ifPhotoIncludeDetails = false)
    {
        if (Pictures == null) return HtmlTag.Empty();

        return Pictures.DbEntry switch
        {
            PhotoContent p => PhotoFigureWithCaptionAndLinkToPageTag(p, sizes, ifPhotoIncludeDetails),
            ImageContent i => ImageFigureWithCaptionAndLinkToPageTag(i, sizes),
            _ => throw new ArgumentException("not a recognized picture type")
        };
    }


    public HtmlTag PictureFigureWithCaptionTag(string sizes, bool ifPhotoIncludeDetails = false)
    {
        if (Pictures == null) return HtmlTag.Empty();

        return Pictures.DbEntry switch
        {
            PhotoContent p => PhotoFigureWithCaptionTag(p, sizes, ifPhotoIncludeDetails),
            ImageContent i => ImageFigureWithCaptionTag(i, sizes),
            _ => throw new ArgumentException("not a recognized picture type", nameof(Pictures.DbEntry))
        };
    }

    public HtmlTag PictureFigureWithLinkToPicturePageTag(string sizes)
    {
        if (Pictures == null) return HtmlTag.Empty();

        return Pictures.DbEntry switch
        {
            PhotoContent => PhotoFigureWithLinkToPageTag(sizes),
            ImageContent => ImageFigureWithLinkToPageTag(sizes),
            _ => throw new ArgumentException("not a recognized picture type")
        };
    }

    public HtmlTag PictureFigureWithTitleCaptionTag(string sizes, bool ifPhotoIncludeDetails = false)
    {
        if (Pictures == null) return HtmlTag.Empty();

        return Pictures.DbEntry switch
        {
            PhotoContent p => PhotoFigureWithTitleCaptionTag(p, sizes, ifPhotoIncludeDetails),
            ImageContent i => ImageFigureWithTitleCaptionTag(i, sizes),
            _ => throw new ArgumentException("not a recognized picture type")
        };
    }
}