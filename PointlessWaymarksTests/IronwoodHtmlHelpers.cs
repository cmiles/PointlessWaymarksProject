using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using NUnit.Framework;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksTests
{
    public static class IronwoodHtmlHelpers
    {
        public static void CheckGenerationVersionEquals(FileInfo htmlFile, DateTime generationVersion)
        {
            var indexDocument = DocumentFromFile(htmlFile);

            var generationVersionAttributeString =
                indexDocument.Head.Attributes.Single(x => x.Name == "data-generationversion").Value;

            Assert.AreEqual(generationVersion.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff"),
                generationVersionAttributeString,
                $"Generation Version of HTML Does not match Data for {htmlFile.Name}");
        }

        public static void CheckGenerationVersionLessThan(FileInfo htmlFile, DateTime generationVersion)
        {
            var indexDocument = DocumentFromFile(htmlFile);

            var generationVersionAttributeString =
                indexDocument.Head.Attributes.Single(x => x.Name == "data-generationversion").Value;

            var generationVersionAttribute = DateTime.Parse(generationVersionAttributeString);

            Assert.Less(generationVersionAttribute, generationVersion,
                $"Expecting a generation version less than {generationVersion:O} but found {generationVersionAttribute:O} for {htmlFile.Name}");
        }


        public static void CheckIndexHtmlAndGenerationVersion(DateTime generationVersion)
        {
            var indexFile = UserSettingsSingleton.CurrentSettings().LocalSiteIndexFile();

            Assert.True(indexFile.Exists, "Index file doesn't exist after generation");

            var indexDocument = DocumentFromFile(indexFile);

            var generationVersionAttributeString =
                indexDocument.Head.Attributes.Single(x => x.Name == "data-generationversion").Value;

            Assert.AreEqual(generationVersion.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff"),
                generationVersionAttributeString, "Content Version of HTML Does not match Data");

            Assert.AreEqual(UserSettingsSingleton.CurrentSettings().SiteName, indexDocument.Title);

            Assert.AreEqual(UserSettingsSingleton.CurrentSettings().SiteSummary,
                indexDocument.QuerySelector("meta[name='description']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            Assert.AreEqual(UserSettingsSingleton.CurrentSettings().SiteAuthors,
                indexDocument.QuerySelector("meta[name='author']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            Assert.AreEqual(UserSettingsSingleton.CurrentSettings().SiteKeywords,
                indexDocument.QuerySelector("meta[name='keywords']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            Assert.AreEqual(UserSettingsSingleton.CurrentSettings().SiteSummary,
                indexDocument.QuerySelector("meta[name='description']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);
        }

        public static async Task CommonContentChecks(IHtmlDocument document, IContentCommon toCheck)
        {
            Assert.AreEqual(toCheck.Title, document.Title);

            var contentVersionAttributeString =
                document.Head.Attributes.Single(x => x.Name == "data-contentversion").Value;

            Assert.AreEqual(toCheck.ContentVersion.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff"),
                contentVersionAttributeString, "Content Version of HTML Does not match Data");

            //Todo - check description

            Assert.AreEqual(toCheck.Tags,
                document.QuerySelector("meta[name='keywords']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            Assert.AreEqual(UserSettingsSingleton.CurrentSettings().SiteName,
                document.QuerySelector("meta[property='og:site_name']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            Assert.AreEqual($"https:{await UserSettingsSingleton.CurrentSettings().PageUrl(toCheck.ContentId)}",
                document.QuerySelector("meta[property='og:url']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);

            Assert.AreEqual("article",
                document.QuerySelector("meta[property='og:type']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value);
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
}