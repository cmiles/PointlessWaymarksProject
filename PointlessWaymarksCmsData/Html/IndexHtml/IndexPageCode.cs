using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsData.Html.FileHtml;
using PointlessWaymarksCmsData.Html.ImageHtml;
using PointlessWaymarksCmsData.Html.NoteHtml;
using PointlessWaymarksCmsData.Html.PhotoHtml;
using PointlessWaymarksCmsData.Html.PostHtml;
using PointlessWaymarksCmsData.Rss;

namespace PointlessWaymarksCmsData.Html.IndexHtml
{
    public partial class IndexPage
    {
        private int _numberOfContentItemsToDisplay = 4;

        public IndexPage()
        {
            var settings = UserSettingsSingleton.CurrentSettings();
            SiteUrl = settings.SiteUrl;
            SiteName = settings.SiteName;
            SiteKeywords = settings.SiteKeywords;
            SiteSummary = settings.SiteSummary;
            SiteAuthors = settings.SiteAuthors;
            PageUrl = settings.IndexPageUrl();

            IndexContent = Db.MainFeedRecentDynamicContent(20).Result.OrderByDescending(x => x.CreatedOn).ToList();

            var mainImageGuid = IndexContent
                .FirstOrDefault(x => x.GetType() == typeof(PostContent) && x.MainPicture != null)?.MainPicture;

            if (mainImageGuid != null) MainImage = new PictureSiteInformation(mainImageGuid);

            if (!IndexContent.Any() || IndexContent.Count <= _numberOfContentItemsToDisplay)
            {
                PreviousPosts = new List<IContentCommon>();
            }
            else
            {
                DateTime previousDate = IndexContent.Skip(_numberOfContentItemsToDisplay - 1).Max(x => x.CreatedOn);

                var previousLater = Tags.MainFeedPreviousAndLaterContent(6, previousDate);

                PreviousPosts = previousLater.previousContent;
            }
        }

        public DateTime? GenerationVersion { get; set; }
        public List<dynamic> IndexContent { get; }
        public PictureSiteInformation MainImage { get; }
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

            foreach (var loopPosts in IndexContent.Take(_numberOfContentItemsToDisplay))
            {
                if (loopPosts.GetType() == typeof(PostContent))
                {
                    var post = new SinglePostDiv(loopPosts);
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

                if (loopPosts.GetType() == typeof(PhotoContent))
                {
                    var post = new SinglePhotoDiv(loopPosts);
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

                if (loopPosts.GetType() == typeof(FileContent))
                {
                    var post = new SingleFileDiv(loopPosts);
                    var indexPostContentDiv = new DivTag().AddClass("index-posts-content");
                    indexPostContentDiv.Encoded(false).Text(post.TransformText());
                    indexBodyContainer.Children.Add(indexPostContentDiv);
                    indexBodyContainer.Children.Add(HorizontalRule.StandardRule());
                }
            }

            return indexBodyContainer;
        }

        public void WriteLocalHtml()
        {
            WriteRss();

            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(TransformText());

            var stringWriter = new StringWriter();
            htmlDoc.ToHtml(stringWriter, new PrettyMarkupFormatter());

            var htmlString = stringWriter.ToString();

            var htmlFileInfo =
                new FileInfo($@"{UserSettingsSingleton.CurrentSettings().LocalSiteRootDirectory}\index.html");

            if (htmlFileInfo.Exists)
            {
                htmlFileInfo.Delete();
                htmlFileInfo.Refresh();
            }

            File.WriteAllText(htmlFileInfo.FullName, htmlString);
        }


        public void WriteRss()
        {
            var items = new List<string>();

            foreach (var loopPosts in IndexContent)
            {
                if (loopPosts.GetType() == typeof(PostContent))
                {
                    var post = new SinglePostDiv(loopPosts);

                    string content = null;

                    if (post.DbEntry.MainPicture != null)
                    {
                        var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                        if (imageInfo != null)
                            content =
                                $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }

                    if (string.IsNullOrWhiteSpace(content))
                        content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                    items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"https:{post.PageUrl}", content,
                        post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
                }

                if (loopPosts.GetType() == typeof(NoteContent))
                {
                    var post = new SingleNoteDiv(loopPosts);

                    var content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                    items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"https:{post.PageUrl}", content,
                        post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
                }

                if (loopPosts.GetType() == typeof(PhotoContent))
                {
                    var post = new SinglePostDiv(loopPosts);

                    string content = null;

                    if (post.DbEntry.MainPicture != null)
                    {
                        var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                        if (imageInfo != null)
                            content =
                                $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                        if (string.IsNullOrWhiteSpace(content))
                            content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                      $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                        items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"https:{post.PageUrl}", content,
                            post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
                    }
                }

                if (loopPosts.GetType() == typeof(ImageContent))
                {
                    var post = new SingleImageDiv(loopPosts);

                    string content = null;

                    if (post.DbEntry.MainPicture != null)
                    {
                        var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                        if (imageInfo != null)
                            content =
                                $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }

                    if (string.IsNullOrWhiteSpace(content))
                        content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                    items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"https:{post.PageUrl}", content,
                        post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
                }

                if (loopPosts.GetType() == typeof(FileContent))
                {
                    var post = new SingleFileDiv(loopPosts);

                    string content = null;

                    if (post.DbEntry.MainPicture != null)
                    {
                        var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);

                        if (imageInfo != null)
                            content =
                                $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }

                    if (string.IsNullOrWhiteSpace(content))
                        content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                    items.Add(RssBuilder.RssItemString(post.DbEntry.Title, $"https:{post.PageUrl}", content,
                        post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
                }
            }

            var localIndexFile = UserSettingsSingleton.CurrentSettings().LocalSiteRssIndexFeedListFile();

            if (localIndexFile.Exists)
            {
                localIndexFile.Delete();
                localIndexFile.Refresh();
            }

            File.WriteAllText(localIndexFile.FullName,
                RssBuilder.RssFileString($"{UserSettingsSingleton.CurrentSettings().SiteName}",
                    string.Join(Environment.NewLine, items)), Encoding.UTF8);
        }
    }
}