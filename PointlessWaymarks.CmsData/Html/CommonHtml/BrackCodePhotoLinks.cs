using System;
using System.Collections.Generic;
using System.Linq;
using HtmlTags;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Html.CommonHtml
{
    public static class BracketCodePhotoLinks
    {
        public const string BracketCodeToken = "photolink";

        public static string Create(PhotoContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static List<PhotoContent> DbContentFromBracketCodes(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<PhotoContent>();

            var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

            var returnList = new List<PhotoContent>();

            if (!guidList.Any()) return returnList;

            var context = Db.Context().Result;

            foreach (var loopGuid in guidList)
            {
                var dbContent = context.PhotoContents.FirstOrDefault(x => x.ContentId == loopGuid);
                if (dbContent == null) continue;

                progress?.Report($"Photo Link Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static string Process(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Photo Link Codes");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = Db.Context().Result;

            foreach (var loopMatch in resultList)
            {
                var dbContent = context.PhotoContents.FirstOrDefault(x => x.ContentId == loopMatch.contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"Adding photo link {dbContent.Title} from Code");
                var settings = UserSettingsSingleton.CurrentSettings();

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(), settings.PhotoPageUrl(dbContent), "photo-page-link");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
            }

            return toProcess;
        }
    }
}