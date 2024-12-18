using System.Text;
using System.Web;
using System.Xml.Linq;
using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.ContentHtml.NoteHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.ContentHtml.PostHtml;
using PointlessWaymarks.CmsData.ContentHtml.TrailHtml;
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
            PreviousPosts = [];
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
    public bool IncludeCodeHighlightingScripts { get; set; }
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

    public string HeaderAdditions()
    {
        var content = IndexContent.Take(UserSettingsSingleton.CurrentSettings().NumberOfItemsOnMainSitePage).ToList();

        var headers = HeaderContentBasedAdditions.HeaderIncludes();

        var headerList = new List<string>();

        foreach (var loopHeader in headers)
        {
            var commonContent = content.Where(x => x is IContentCommon).Cast<IContentCommon>().ToList();

            if (commonContent.Any(x => loopHeader.IsNeeded(x)))
            {
                headerList.Add(loopHeader.HeaderAdditions());
                continue;
            }

            var notCommonContent = content.Where(x => x is not IContentCommon).ToList();

            foreach (var loopContent in notCommonContent)
                if (DynamicTypeTools.PropertyExists(loopContent, "BodyContent"))
                    if (loopHeader.IsNeeded((string?)loopContent.BodyContent))
                    {
                        headerList.Add(loopHeader.HeaderAdditions());
                        break;
                    }
        }

        return string.Join(Environment.NewLine, headerList);
    }

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

            if (loopPosts.GetType() == typeof(TrailContent))
            {
                var trail = new SingleTrailDiv(loopPosts);
                var indexTrailContentDiv = new DivTag().AddClass("index-trails-content");
                indexTrailContentDiv.Encoded(false).Text(trail.TransformText());
                indexBodyContainer.Children.Add(indexTrailContentDiv);
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


        //foreach (var loopPosts in
        //         IndexContent.Take(UserSettingsSingleton.CurrentSettings().NumberOfItemsOnMainSitePage))
        //{
        //    if (DynamicTypeTools.PropertyExists(loopPosts, "BodyContent"))
        //    {
        //        if (!IncludeSpatialScripts)
        //            IncludeSpatialScripts = SpatialScripts.IsNeeded((string?)loopPosts.BodyContent);
        //        if (!IncludeCodeHighlightingScripts)
        //            IncludeCodeHighlightingScripts = CodeHighlightingScripts.IsNeeded((string?)loopPosts.BodyContent);
        //    }

        //    if (loopPosts.GetType() == typeof(PointContentDto) || loopPosts.GetType() == typeof(GeoJsonContent) ||
        //        loopPosts.GetType() == typeof(LineContent))
        //        IncludeSpatialScripts = true;
        //}

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
        var items = new List<XElement>();

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

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}",
                    Tags.CreatedByAndUpdatedByNameList(post.DbEntry), content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }

            if (loopPosts.GetType() == typeof(NoteContent))
            {
                var post = new SingleNoteDiv(loopPosts);

                var content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}",
                    Tags.CreatedByAndUpdatedByNameList(post.DbEntry), content,
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

                    items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}",
                        Tags.CreatedByAndUpdatedByNameList(post.DbEntry), content,
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

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}",
                    Tags.CreatedByAndUpdatedByNameList(post.DbEntry), content,
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

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}",
                    Tags.CreatedByAndUpdatedByNameList(post.DbEntry), content,
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

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}",
                    Tags.CreatedByAndUpdatedByNameList(post.DbEntry), content,
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

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", 
                    Tags.CreatedByAndUpdatedByNameList(post.DbEntry), content,
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

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", 
                    Tags.CreatedByAndUpdatedByNameList(post.DbEntry), content,
                    post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
            }

            if (loopPosts.GetType() == typeof(TrailContent))
            {
                var trail = new SingleTrailDiv(loopPosts);

                string? content = null;

                if (trail.DbEntry.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(trail.DbEntry.MainPicture.Value);

                    if (imageInfo != null)
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(trail.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"{trail.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                if (string.IsNullOrWhiteSpace(content))
                    content = $"<p>{HttpUtility.HtmlEncode(trail.DbEntry.Summary)}</p>" +
                              $"<p>Read more at <a href=\"{trail.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssBuilder.RssItemString(trail.DbEntry.Title, $"{trail.PageUrl}",
                    Tags.CreatedByAndUpdatedByNameList(trail.DbEntry), content,
                    trail.DbEntry.CreatedOn, trail.DbEntry.ContentId.ToString()));
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

                items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"{post.PageUrl}", 
                    Tags.CreatedByAndUpdatedByNameList(post.DbEntry), content,
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
                items), Encoding.UTF8).ConfigureAwait(false);
    }
}