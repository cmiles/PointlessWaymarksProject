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
        public static async Task CommonContentChecks(IHtmlDocument document, IContentCommon toCheck)
        {
            Assert.AreEqual(toCheck.Title, document.Title);

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