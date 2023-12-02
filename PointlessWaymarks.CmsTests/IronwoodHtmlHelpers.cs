using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using NUnit.Framework;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsTests;

public static class IronwoodHtmlHelpers
{
    public static void CheckGenerationVersionEquals(FileInfo htmlFile, DateTime generationVersion)
    {
        var indexDocument = DocumentFromFile(htmlFile);

        var generationVersionAttributeString =
            indexDocument.Head.Attributes.Single(x => x.Name == "data-generationversion").Value;

        Assert.That(generationVersionAttributeString, Is.EqualTo(generationVersion.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff")),
            $"Generation Version of HTML Does not match Data for {htmlFile.Name}");
    }

    public static void CheckGenerationVersionLessThan(FileInfo htmlFile, DateTime generationVersion)
    {
        var indexDocument = DocumentFromFile(htmlFile);

        var generationVersionAttributeString =
            indexDocument.Head.Attributes.Single(x => x.Name == "data-generationversion").Value;

        var generationVersionAttribute = DateTime.Parse(generationVersionAttributeString);

        Assert.That(generationVersionAttribute, Is.LessThan(generationVersion),
            $"Expecting a generation version less than {generationVersion:O} but found {generationVersionAttribute:O} for {htmlFile.Name}");
    }


    public static void CheckIndexHtmlAndGenerationVersion(DateTime generationVersion)
    {
        var indexFile = UserSettingsSingleton.CurrentSettings().LocalSiteIndexFile();

        Assert.That(indexFile.Exists, "Index file doesn't exist after generation");

        var indexDocument = DocumentFromFile(indexFile);

        var generationVersionAttributeString =
            indexDocument.Head.Attributes.Single(x => x.Name == "data-generationversion").Value;

        Assert.Multiple(() =>
        {
            Assert.That(generationVersionAttributeString, Is.EqualTo(generationVersion.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff")), "Content Version of HTML Does not match Data");

            Assert.That(indexDocument.Title, Is.EqualTo(UserSettingsSingleton.CurrentSettings().SiteName));

            Assert.That(indexDocument.QuerySelector("meta[name='description']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value, Is.EqualTo(UserSettingsSingleton.CurrentSettings().SiteSummary));

            Assert.That(indexDocument.QuerySelector("meta[name='author']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value, Is.EqualTo(UserSettingsSingleton.CurrentSettings().SiteAuthors));

            Assert.That(indexDocument.QuerySelector("meta[name='keywords']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value, Is.EqualTo(UserSettingsSingleton.CurrentSettings().SiteKeywords));
        });

        Assert.That(indexDocument.QuerySelector("meta[name='description']")?.Attributes
                .FirstOrDefault(x => x.LocalName == "content")?.Value, Is.EqualTo(UserSettingsSingleton.CurrentSettings().SiteSummary));
    }

    public static async Task CommonContentChecks(IHtmlDocument document, IContentCommon toCheck)
    {
        Assert.That(document.Title, Is.EqualTo(toCheck.Title));

        var contentVersionAttributeString =
            document.Head.Attributes.Single(x => x.Name == "data-contentversion").Value;

        Assert.Multiple(async () =>
        {
            Assert.That(contentVersionAttributeString, Is.EqualTo(toCheck.ContentVersion.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff")), "Content Version of HTML Does not match Data");

            //Todo - check description

            Assert.That(document.QuerySelector("meta[name='keywords']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value, Is.EqualTo(toCheck.Tags));

            Assert.That(document.QuerySelector("meta[property='og:site_name']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value, Is.EqualTo(UserSettingsSingleton.CurrentSettings().SiteName));

            Assert.That(document.QuerySelector("meta[property='og:url']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value, Is.EqualTo($"{await UserSettingsSingleton.CurrentSettings().PageUrl(toCheck.ContentId)}"));

            Assert.That(document.QuerySelector("meta[property='og:type']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value, Is.EqualTo("article"));
        });

        var tagContainers = document.QuerySelectorAll(".tags-detail-link-container");
        var contentTags = Db.TagListParseToSlugsAndIsExcluded(toCheck);
        Assert.That(contentTags.Count, Is.EqualTo(tagContainers.Length));

        var tagLinks = document.QuerySelectorAll(".tag-detail-link");
        Assert.That(contentTags.Count(x => !x.IsExcluded), Is.EqualTo(tagLinks.Length));

        var tagNoLinks = document.QuerySelectorAll(".tag-detail-text");
        Assert.That(contentTags.Count(x => x.IsExcluded), Is.EqualTo(tagNoLinks.Length));
    }

    public static IHtmlDocument DocumentFromFile(FileInfo htmlFile)
    {
        var config = Configuration.Default;

        var context = BrowsingContext.New(config);
        var parser = context.GetService<IHtmlParser>();
        var source = File.ReadAllText(htmlFile.FullName);
        return parser.ParseDocument(source);
    }
}