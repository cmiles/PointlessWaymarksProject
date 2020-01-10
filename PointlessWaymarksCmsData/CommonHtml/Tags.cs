using System.Linq;
using HtmlTags;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class Tags
    {
        public static string CssStyleFileString()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;
            return $"<link rel=\"stylesheet\" href=\"{settings.CssMainStyleFileUrl()}?v=1.0\">";
        }

        public static string FavIconFileString()
        {
            var settings = UserSettingsUtilities.ReadSettings().Result;
            return $"<link rel=\"shortcut icon\" href=\"{settings.FaviconUrl()}\">";
        }

        public static HtmlTag InfoDivTag(string contents, string className, string dataType, string dataValue)
        {
            if (string.IsNullOrWhiteSpace(contents)) return HtmlTag.Empty();
            var divTag = new HtmlTag("div");
            divTag.AddClass(className);

            var spanTag = new HtmlTag("div");
            spanTag.Text(contents.Trim());
            spanTag.AddClass($"{className}-content");
            spanTag.Data(dataType, dataValue);

            divTag.Children.Add(spanTag);

            return divTag;
        }

        public static HtmlTag TagList(ITag dbEntry)
        {
            var tagsContainer = new DivTag().AddClass("tags-container");

            if (string.IsNullOrWhiteSpace(dbEntry.Tags)) return HtmlTag.Empty();

            tagsContainer.Children.Add(new DivTag().Text("Tags:").AddClass("tag-detail-label-tag"));

            var tags = dbEntry.Tags.Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();

            if (!tags.Any()) return HtmlTag.Empty();

            foreach (var loopTag in tags) tagsContainer.Children.Add(InfoDivTag(loopTag, "tag-detail", "tag", loopTag));

            return tagsContainer;
        }
    }
}