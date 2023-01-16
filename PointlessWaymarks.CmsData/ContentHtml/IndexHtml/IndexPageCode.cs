using System.Text;
using System.Web;
using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.ContentHtml.NoteHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.ContentHtml.PostHtml;
using PointlessWaymarks.CmsData.ContentHtml.VideoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Rss;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.ContentHtml.IndexHtml;

public partial class IndexPage
{
    public IndexPage()
    {
        var settings = UserSettingsSingleton.CurrentSettings();
        SiteUrl = settings.SiteUrl();
        SiteName = settings.SiteName;
        SiteKeywords = settings.SiteKeywords;
        SiteSummary = settings.SiteSummary;
        SiteAuthors = settings.SiteAuthors;
        PageUrl = settings.IndexPageUrl();
        LangAttribute = settings.SiteLangAttribute;
        DirAttribute = settings.SiteDirectionAttribute;

        IndexContent = Db.MainFeedRecentDynamicContent(20).Result.OrderByDescending(x => x.FeedOn).ToList();

        var mainImageGuid = IndexContent
            .FirstOrDefault(x => x.GetType() == typeof(PostContent) && x.MainPicture != null)?.MainPicture;

        if (mainImageGuid != null) MainImage = new PictureSiteInformation(mainImageGuid);

        if (!IndexContent.Any() || IndexContent.Count <= settings.NumberOfItemsOnMainSitePage)
        {
            PreviousPosts = new List<IContentCommon>();
        }
        else
        {
            DateTime previousDate = IndexContent.Skip(settings.NumberOfItemsOnMainSitePage - 1).Max(x => x.CreatedOn);

            var previousLater = Tags.MainFeedPreviousAndLaterContent(6, previousDate);

            PreviousPosts = previousLater.previousContent;
        }
    }

    public string DirAttribute { get; set; }

    public DateTime? GenerationVersion { get; set; }
    public bool IncludeSpatialScripts { get; set; }
    public List<dynamic> IndexContent { get; }

    public string LangAttribute { get; set; }
    public PictureSiteInformation? MainImage { get; }
    public string PageUrl { get; }
    public List<IContentCommon> PreviousPosts { get; }
    public string SiteAuthors { get; }
    public string SiteKeywords { get; }
    public string SiteName { get; }
    public string SiteSummary { get; }
    public string SiteUrl { get; }

    public HtmlTag IndexPosts()
    {
        if (!IndexContent.Any()) return HtmlTag.Empty();

        var indexBodyContainer = new DivTag().AddClass("index-posts-container");

        //!!Content Type List!!
        foreach (var loopPosts in
                 IndexContent.Take(UserSettingsSingleton.CurrentSettings().NumberOfItemsOnMainSitePage))
        {
            if (loopPosts.GetType() == typeof(FileContent))
            {
                var post = new SingleFileDiv(loopPosts);
                var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                indexPostContentDiv.Encoded(false).Text(post.TransformText());
                indexBodyContainer.Children.Add(indexPostContentDiv);
                indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
            }

            if (loopPosts.GetType() == typeof(ImageContent))
            {
                var post = new SingleImageDiv(loopPosts);
                var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                indexPostContentDiv.Encoded(false).Text(post.TransformText());
                indexBodyContainer.Children.Add(indexPostContentDiv);
                indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
            }

            if (loopPosts.GetType() == typeof(GeoJsonContent))
            {
                var post = new SingleGeoJsonDiv(loopPosts);
                var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                indexPostContentDiv.Encoded(false).Text(post.TransformText());
                indexBodyContainer.Children.Add(indexPostContentDiv);
                indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
            }

            if (loopPosts.GetType() == typeof(LineContent))
            {
                var post = new SingleLineDiv(loopPosts);
                var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                indexPostContentDiv.Encoded(false).Text(post.TransformText());
                indexBodyContainer.Children.Add(indexPostContentDiv);
                indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
            }

            if (loopPosts.GetType() == typeof(NoteContent))
            {
                var post = new SingleNoteDiv(loopPosts);
                var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                indexPostContentDiv.Encoded(false).Text(post.TransformText());
                indexBodyContainer.Children.Add(indexPostContentDiv);
                indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
            }

            if (loopPosts.GetType() == typeof(PointContentDto))
            {
                var post = new SinglePointDiv(loopPosts);
                var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                indexPostContentDiv.Encoded(false).Text(post.TransformText());
                indexBodyContainer.Children.Add(indexPostContentDiv);
                indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
            }

            if (loopPosts.GetType() == typeof(PhotoContent))
            {
                var post = new SinglePhotoDiv(loopPosts);
                var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                indexPostContentDiv.Encoded(false).Text(post.TransformText());
                indexBodyContainer.Children.Add(indexPostContentDiv);
                indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
            }

            if (loopPosts.GetType() == typeof(PostContent))
            {
                var post = new SinglePostDiv(loopPosts);
                var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                indexPostContentDiv.Encoded(false).Text(post.TransformText());
                indexBodyContainer.Children.Add(indexPostContentDiv);
                indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
            }

            if (loopPosts.GetType() == typeof(VideoContent))
            {
                var post = new SingleVideoDiv(loopPosts);
                var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                indexPostContentDiv.Encoded(false).Text(post.TransformText());
                indexBodyContainer.Children.Add(indexPostContentDiv);
                indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
            }
        }

        return indexBodyContainer;
    }

    public async Task WriteLocalHtml()
    {
        await WriteRss().ConfigureAwait(false);

        foreach (var loopPosts in
                 IndexContent.Take(UserSettingsSingleton.CurrentSettings().NumberOfItemsOnMainSitePage))
        {
            if (DynamicTypeTools.PropertyExists(loopPosts, "Body") &&
                BracketCodeCommon.ContainsSpatialScriptDependentBracketCodes((string)loopPosts.Body))
                IncludeSpatialScripts = true;
            if (loopPosts.GetType() == typeof(PointContentDto) || loopPosts.GetType() == typeof(GeoJsonContent) ||
                loopPosts.GetType() == typeof(LineContent))
                IncludeSpatialScripts = true;
        }

        var htmlString = TransformText();

        var htmlFileInfo =
            new FileInfo(
                $@"{UserSettingsSingleton.CurrentSettings().LocalSiteRootFullDirectory().FullName}\index.html");

        if (htmlFileInfo.Exists)
        {
            htmlFileInfo.Delete();
            htmlFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLog(htmlFileInfo.FullName, htmlString).ConfigureAwait(false);
    }


    public async Task WriteRss()
    {
        var items = new List<string>();

        //!!Content Type List!!
        foreach (var loopPosts in IndexContent)
        {
            if (loopPosts.GetType() == typeof(PostContent))
            {
                var post = new SinglePostDiv(loopPosts);

                string? content = null;

                if (post.DbEntry.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                    if (imageInfo != null)
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                if (string.IsNullOrWhiteSpace(content))
                    content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }

            if (loopPosts.GetType() == typeof(NoteContent))
            {
                var post = new SingleNoteDiv(loopPosts);

                var content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }

            if (loopPosts.GetType() == typeof(PhotoContent))
            {
                var post = new SinglePhotoDiv(loopPosts);

                string? content = null;

                if (post.DbEntry.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                    if (imageInfo != null)
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                    if (string.IsNullOrWhiteSpace(content))
                        content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                    items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", content,
                        post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
                }
            }

            if (loopPosts.GetType() == typeof(ImageContent))
            {
                var post = new SingleImageDiv(loopPosts);

                string? content = null;

                if (post.DbEntry.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                    if (imageInfo != null)
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                if (string.IsNullOrWhiteSpace(content))
                    content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }

            if (loopPosts.GetType() == typeof(FileContent))
            {
                var post = new SingleFileDiv(loopPosts);

                string? content = null;

                if (post.DbEntry.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                    if (imageInfo != null)
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                if (string.IsNullOrWhiteSpace(content))
                    content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }

            if (loopPosts.GetType() == typeof(PointContentDto))
            {
                var post = new SinglePointDiv(loopPosts);

                string? content = null;

                if (post.DbEntry.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                    if (imageInfo != null)
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                if (string.IsNullOrWhiteSpace(content))
                    content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }

            if (loopPosts.GetType() == typeof(LineContent))
            {
                var post = new SingleLineDiv(loopPosts);

                string? content = null;

                if (post.DbEntry.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                    if (imageInfo != null)
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                if (string.IsNullOrWhiteSpace(content))
                    content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }

            if (loopPosts.GetType() == typeof(GeoJsonContent))
            {
                var post = new SingleGeoJsonDiv(loopPosts);

                string? content = null;

                if (post.DbEntry.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                    if (imageInfo != null)
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                if (string.IsNullOrWhiteSpace(content))
                    content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }

            if (loopPosts.GetType() == typeof(VideoContent))
            {
                var post = new SingleVideoDiv(loopPosts);

                string? content = null;

                if (post.DbEntry.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                    if (imageInfo != null)
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                if (string.IsNullOrWhiteSpace(content))
                    content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }
        }

        var localIndexFile = UserSettingsSingleton.CurrentSettings().LocalSiteRssIndexFeedListFile();

        if (localIndexFile.Exists)
        {
            localIndexFile.Delete();
            localIndexFile.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLog(localIndexFile.FullName,
            RssBuilder.RssFileString($"{UserSettingsSingleton.CurrentSettings().SiteName}",
                string.Join(Environment.NewLine, items)), Encoding.UTF8).ConfigureAwait(false);
    }
}