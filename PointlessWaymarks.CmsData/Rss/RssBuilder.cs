﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Rss
{
    public static class RssBuilder
    {
        public static string RssFileString(string channelTitle, string items)
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

        public static string RssItemString(string? title, string? link, string? content, DateTime createdOn,
            string contentId)
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

        public static async void WriteContentCommonListRss(List<IContentCommon> content, FileInfo fileInfo,
            string titleAdd, IProgress<string>? progress = null)
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var items = new List<string>();

            progress?.Report($"Processing {content.Count} Content Entries to write to {titleAdd} RSS");

            foreach (var loopContent in content)
            {
                var contentUrl = await settings.ContentUrl(loopContent.ContentId);

                string? itemDescription = null;

                if (loopContent.MainPicture != null)
                {
                    var imageInfo = PictureAssetProcessing.ProcessPictureDirectory(loopContent.MainPicture.Value);

                    if (imageInfo != null)
                        itemDescription =
                            $"{Tags.PictureImgTagDisplayImageOnly(imageInfo)}<p>{HttpUtility.HtmlEncode(loopContent.Summary)}</p>" +
                            $"<p>Read more at <a href=\"https:{contentUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";
                }

                if (string.IsNullOrWhiteSpace(itemDescription))
                    itemDescription = $"<p>{HttpUtility.HtmlEncode(loopContent.Summary)}</p>" +
                                      $"<p>Read more at <a href=\"https:{contentUrl}\">{UserSettingsSingleton.CurrentSettings().SiteName}</a></p>";

                items.Add(RssItemString(loopContent.Title, $"https:{contentUrl}", itemDescription,
                    loopContent.CreatedOn, loopContent.ContentId.ToString()));
            }

            progress?.Report($"Writing {titleAdd} RSS to {fileInfo.FullName}");

            if (fileInfo.Exists)
            {
                fileInfo.Delete();
                fileInfo.Refresh();
            }

            await FileManagement.WriteAllTextToFileAndLogAsync(fileInfo.FullName,
                RssFileString($"{UserSettingsSingleton.CurrentSettings().SiteName} - {titleAdd}",
                    string.Join(Environment.NewLine, items)), Encoding.UTF8);
        }
    }
}