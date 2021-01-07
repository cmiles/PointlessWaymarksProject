using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using pinboard.net;
using pinboard.net.Models;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.LinkListHtml;

namespace PointlessWaymarks.CmsData.Content
{
    public static class LinkGenerator
    {
        public static void GenerateHtmlAndJson(DateTime? generationVersion, IProgress<string> progress)
        {
            progress?.Report("Link Content - Generate HTML");

            var htmlContext = new LinkListPage {GenerationVersion = generationVersion};

            htmlContext.WriteLocalHtmlRssAndJson();
        }

        public static async Task<(GenerationReturn generationReturn, LinkMetadata metadata)> LinkMetadataFromUrl(
            string url, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(url)) return (await GenerationReturn.Error("No URL?"), null);

            progress?.Report("Setting up and Downloading Site");

            var toReturn = new LinkMetadata();

            var config = Configuration.Default.WithDefaultLoader().WithJs();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(url);

            progress?.Report("Looking for Title");

            var titleString = document.Head?.Children.FirstOrDefault(x => x.TagName == "TITLE")?.TextContent;

            if (string.IsNullOrWhiteSpace(titleString))
                titleString = document.QuerySelector("meta[property='og:title']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(titleString))
                titleString = document.QuerySelector("meta[name='DC.title']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(titleString))
                titleString = document.QuerySelector("meta[name='twitter:title']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "value")?.Value;

            if (!string.IsNullOrWhiteSpace(titleString)) toReturn.Title = titleString;

            progress?.Report("Looking for Author");

            var authorString = document.QuerySelector("meta[property='og:author']")?.Attributes
                .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector("meta[name='DC.contributor']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector("meta[property='article:author']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector("meta[name='author']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector("a[rel~=\"author\"]")?.TextContent;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector(".author__name")?.TextContent;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector(".author_name")?.TextContent;

            if (!string.IsNullOrWhiteSpace(authorString)) toReturn.Author = authorString;

            progress?.Report($"Looking for Author - Found {toReturn.Author}");


            progress?.Report("Looking for Date Time");

            var linkDateString = document.QuerySelector("meta[property='article:modified_time']")?.Attributes
                .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(linkDateString))
                linkDateString = document.QuerySelector("meta[property='og:updated_time']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(linkDateString))
                linkDateString = document.QuerySelector("meta[property='article:published_time']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(linkDateString))
                linkDateString = document.QuerySelector("meta[property='article:published_time']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(linkDateString))
                linkDateString = document.QuerySelector("meta[name='DC.date.created']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            progress?.Report($"Looking for Date Time - Found {linkDateString}");

            if (!string.IsNullOrWhiteSpace(linkDateString))
            {
                if (DateTime.TryParse(linkDateString, out var parsedDateTime))
                {
                    toReturn.LinkDate = parsedDateTime;
                    progress?.Report($"Looking for Date Time - Parsed to {parsedDateTime}");
                }
                else
                {
                    progress?.Report("Did not parse Date Time");
                }
            }

            progress?.Report("Looking for Site Name");

            var siteString = document.QuerySelector("meta[property='og:site_name']")?.Attributes
                .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(siteString))
                siteString = document.QuerySelector("meta[name='DC.publisher']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(siteString))
                siteString = document.QuerySelector("meta[name='twitter:site']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "value")?.Value.Replace("@", "");

            if (!string.IsNullOrWhiteSpace(siteString)) toReturn.Site = siteString;

            progress?.Report($"Looking for Site Name - Found {toReturn.Site}");

            progress?.Report("Looking for Description");

            var descriptionString = document.QuerySelector("meta[name='description']")?.Attributes
                .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(descriptionString))
                descriptionString = document.QuerySelector("meta[property='og:description']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(descriptionString))
                descriptionString = document.QuerySelector("meta[name='twitter:description']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (!string.IsNullOrWhiteSpace(descriptionString)) toReturn.Description = descriptionString;

            progress?.Report($"Looking for Description - Found {toReturn.Description}");

            return (await GenerationReturn.Success($"Parsed URL Metadata for {url} without error"), toReturn);
        }

        public static async Task<(GenerationReturn generationReturn, LinkContent linkContent)> SaveAndGenerateHtml(
            LinkContent toSave, DateTime? generationVersion, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await Db.SaveLinkContent(toSave);
            await SaveLinkToPinboard(toSave, progress);
            GenerateHtmlAndJson(generationVersion, progress);

            DataNotifications.PublishDataNotification("Link Generator", DataNotificationContentType.Link,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (
                await GenerationReturn.Success($"Saved and Generated Content And Html for Links to Add {toSave.Title}"),
                toSave);
        }

        public static async Task<GenerationReturn> SaveLinkToPinboard(LinkContent toSave, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().PinboardApiToken))
                return await GenerationReturn.Success("No PinboardApiToken - skipping save to Pinboard",
                    toSave.ContentId);

            var descriptionFragments = new List<string>();
            if (!string.IsNullOrWhiteSpace(toSave.Site)) descriptionFragments.Add($"Site: {toSave.Site}");
            if (toSave.LinkDate != null) descriptionFragments.Add($"Date: {toSave.LinkDate.Value:g}");
            if (!string.IsNullOrWhiteSpace(toSave.Description))
                descriptionFragments.Add($"Description: {toSave.Description}");
            if (!string.IsNullOrWhiteSpace(toSave.Comments)) descriptionFragments.Add($"Comments: {toSave.Comments}");
            if (!string.IsNullOrWhiteSpace(toSave.Author)) descriptionFragments.Add($"Author: {toSave.Author}");

            var tagList = Db.TagListParse(toSave.Tags);
            tagList.Add(UserSettingsSingleton.CurrentSettings().SiteName);
            tagList = tagList.Select(x => x.Replace(" ", "-")).ToList();

            progress?.Report("Setting up Pinboard");

            var bookmark = new Bookmark
            {
                Url = toSave.Url,
                Description = toSave.Title,
                Extended = string.Join(" ;; ", descriptionFragments),
                Tags = tagList,
                CreatedDate = DateTime.Now,
                Shared = true,
                ToRead = false,
                Replace = true
            };

            try
            {
                using var pb = new PinboardAPI(UserSettingsSingleton.CurrentSettings().PinboardApiToken);
                progress?.Report("Adding Pinboard Bookmark");
                await pb.Posts.Add(bookmark);
            }
            catch (Exception e)
            {
                return await GenerationReturn.Error("Trouble Saving to Pinboard", toSave.ContentId, e);
            }

            progress?.Report("Pinboard Bookmark Complete");

            return await GenerationReturn.Success("Saved to Pinboard", toSave.ContentId);
        }

        public static async Task<GenerationReturn> Validate(LinkContent linkContent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    linkContent.ContentId);

            if (linkContent == null) return await GenerationReturn.Error("Link Content is Null?");

            var (createdUpdatedValid, createdUpdatedValidationMessage) =
                CommonContentValidation.ValidateCreatedAndUpdatedBy(linkContent, linkContent.Id < 1);

            if (!createdUpdatedValid)
                return await GenerationReturn.Error(createdUpdatedValidationMessage, linkContent.ContentId);

            var urlValidation =
                await CommonContentValidation.ValidateLinkContentLinkUrl(linkContent.Url, linkContent.ContentId);

            if (!urlValidation.isValid)
                return await GenerationReturn.Error(urlValidation.explanation, linkContent.ContentId);

            return await GenerationReturn.Success("Link Content Validation Successful");
        }

        public class LinkMetadata
        {
            public string Author { get; set; }

            public string Description { get; set; }
            public DateTime? LinkDate { get; set; }
            public string Site { get; set; }
            public string Title { get; set; }
        }
    }
}