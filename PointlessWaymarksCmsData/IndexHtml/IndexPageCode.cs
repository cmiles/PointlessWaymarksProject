using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.FileHtml;
using PointlessWaymarksCmsData.ImageHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.NoteHtml;
using PointlessWaymarksCmsData.PhotoHtml;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsData.PostHtml;

namespace PointlessWaymarksCmsData.IndexHtml
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

            DateTime previousDate = IndexContent.Skip(_numberOfContentItemsToDisplay - 1).Max(x => x.CreatedOn);

            var previousLater = RelatedPostContent.PreviousAndLaterContent(6, previousDate).Result;

            PreviousPosts = previousLater.previousContent;
        }

        public List<dynamic> IndexContent { get; }


        public PictureSiteInformation MainImage { get; }

        public string PageUrl { get; }

        public List<IContentCommon> PreviousPosts { get; set; }

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


        public void PostProcessOutputBuffer(StringBuilder buffer)
        {
            var xmlDoc = XDocument.Parse(buffer.ToString());
            foreach (var element in xmlDoc.Descendants("channel").First().Descendants("item")
                .Descendants("description"))
                VerifyCdataHtmlEncoding(buffer, element);

            foreach (var element in xmlDoc.Descendants("channel").First().Descendants("description"))
                VerifyCdataHtmlEncoding(buffer, element);

            buffer.Replace(" xmlns:a10=\"http://www.w3.org/2005/Atom\"", " xmlns:atom=\"http://www.w3.org/2005/Atom\"");
            buffer.Replace("a10:", "atom:");
        }

        public static string RssItem(string title, string link, string content, DateTime createdOn, string contentId)
        {
            var rssBuilder = new StringBuilder();

            rssBuilder.AppendLine("    <item>");
            rssBuilder.AppendLine($"        <title>{title}</title>");
            rssBuilder.AppendLine($"        <link>{link}</link>");
            rssBuilder.AppendLine($"        <description><![CDATA[{content}]]></description>");
            rssBuilder.AppendLine($"        <pubDate>{createdOn:R}</pubDate>");
            rssBuilder.AppendLine($"        <guid isPermaLink=\"false\">{contentId}</guid>");
            rssBuilder.AppendLine("    </item>");

            return rssBuilder.ToString();
        }

        public static string RssString(string channelTitle, string items)
        {
            var rssBuilder = new StringBuilder();
            var settings = UserSettingsSingleton.CurrentSettings();

            rssBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            rssBuilder.AppendLine("<!--");
            rssBuilder.AppendLine($"    RSS generated by Pointless Waymarks CMS on {DateTime.Now:R}");
            rssBuilder.AppendLine("-->");
            rssBuilder.AppendLine("<rss version=\"2.0\">");
            rssBuilder.AppendLine("<channel>");
            rssBuilder.AppendLine($"<title>{channelTitle}</title>");
            rssBuilder.AppendLine($"<link>https://{settings.SiteUrl}</link>");
            rssBuilder.AppendLine($"<description>{settings.SiteSummary}</description>");
            rssBuilder.AppendLine("<language>en-us</language>");
            rssBuilder.AppendLine($"<copyright>{DateTime.Now.Year} {settings.SiteAuthors}</copyright>");
            rssBuilder.AppendLine($"<lastBuildDate>{DateTime.Now:R}</lastBuildDate>");
            rssBuilder.AppendLine("<generator>Pointless Waymarks CMS</generator>");
            rssBuilder.AppendLine($"<managingEditor>{settings.SiteEmailTo}</managingEditor>");
            rssBuilder.AppendLine($"<webMaster>{settings.SiteEmailTo}</webMaster>");
            rssBuilder.AppendLine(items);
            rssBuilder.AppendLine("</channel>");
            rssBuilder.AppendLine("</rss>");

            return rssBuilder.ToString();
        }

        public HtmlTag Title()
        {
            var titleContainer = new DivTag().AddClass("index-title-container");
            var titleHeader = new HtmlTag("H1").AddClass("index-title-content").Text(SiteName);
            var titleSiteSummary = new HtmlTag("H5").AddClass("index-title-summary-content").Text(SiteSummary);

            titleContainer.Children.Add(titleHeader);
            titleContainer.Children.Add(titleSiteSummary);

            return titleContainer;
        }

        private static void VerifyCdataHtmlEncoding(StringBuilder buffer, XElement element)
        {
            if (!element.Value.Contains("<") || !element.Value.Contains(">")) return;

            var cdataValue = string.Format("<{0}><![CDATA[{1}]]></{2}>", element.Name, element.Value, element.Name);
            buffer.Replace(element.ToString(), cdataValue);
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

                    var content = string.Empty;

                    if (post.DbEntry.MainPicture != null)
                    {
                        var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }
                    else
                    {
                        content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }

                    items.Add(RssItem(post.DbEntry.Title, $"https:{post.PageUrl}", content, post.DbEntry.CreatedOn,
                        post.DbEntry.ContentId.ToString()));
                }

                if (loopPosts.GetType() == typeof(NoteContent))
                {
                    var post = new SingleNoteDiv(loopPosts);

                    var content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                    items.Add(RssItem(NoteParts.TitleString(post.DbEntry), $"https:{post.PageUrl}", content,
                        post.DbEntry.CreatedOn, post.DbEntry.ContentId.ToString()));
                }

                if (loopPosts.GetType() == typeof(PhotoContent))
                {
                    var post = new SinglePostDiv(loopPosts);

                    var content = string.Empty;

                    if (post.DbEntry.MainPicture != null)
                    {
                        var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }
                    else
                    {
                        content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }

                    items.Add(RssItem(post.DbEntry.Title, $"https:{post.PageUrl}", content, post.DbEntry.CreatedOn,
                        post.DbEntry.ContentId.ToString()));
                }

                if (loopPosts.GetType() == typeof(ImageContent))
                {
                    var post = new SinglePostDiv(loopPosts);

                    var content = string.Empty;

                    if (post.DbEntry.MainPicture != null)
                    {
                        var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }
                    else
                    {
                        content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }

                    items.Add(RssItem(post.DbEntry.Title, $"https:{post.PageUrl}", content, post.DbEntry.CreatedOn,
                        post.DbEntry.ContentId.ToString()));
                }

                if (loopPosts.GetType() == typeof(FileContent))
                {
                    var post = new SinglePostDiv(loopPosts);

                    var content = string.Empty;

                    if (post.DbEntry.MainPicture != null)
                    {
                        var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(post.DbEntry.MainPicture.Value);
                        content =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                            $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }
                    else
                    {
                        content = $"<p>{HttpUtility.HtmlEncode(post.DbEntry.Summary)}</p>" +
                                  $"<p>Read more at <a href=\"https:{post.PageUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                    }

                    items.Add(RssItem(post.DbEntry.Title, $"https:{post.PageUrl}", content, post.DbEntry.CreatedOn,
                        post.DbEntry.ContentId.ToString()));
                }
            }

            var localIndexFile = UserSettingsSingleton.CurrentSettings().LocalSiteRssIndexFeedListFile();

            if (localIndexFile.Exists)
            {
                localIndexFile.Delete();
                localIndexFile.Refresh();
            }

            File.WriteAllText(localIndexFile.FullName,
                RssString($"{UserSettingsSingleton.CurrentSettings().SiteName}",
                    string.Join(Environment.NewLine, items)), Encoding.UTF8);
        }
    }
}