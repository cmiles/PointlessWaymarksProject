﻿using System;
using System.Collections.Generic;
using System.Linq;
using HtmlTags;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.LineHtml;

namespace PointlessWaymarks.CmsData.Html.CommonHtml
{
    public static class BracketCodeLines
    {
        public const string BracketCodeToken = "line";

        public static string Create(LineContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static List<LineContent> DbContentFromBracketCodes(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<LineContent>();

            progress?.Report("Searching for Point Codes...");

            var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

            var returnList = new List<LineContent>();

            if (!guidList.Any()) return returnList;

            var context = Db.Context().Result;

            foreach (var loopMatch in guidList)
            {
                var dbContent = context.LineContents.FirstOrDefault(x => x.ContentId == loopMatch);
                if (dbContent == null) continue;

                progress?.Report($"Line Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static string Process(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Line Codes...");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = Db.Context().Result;

            foreach (var loopMatch in resultList)
            {
                var dbContent = context.LineContents.FirstOrDefault(x => x.ContentId == loopMatch.contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"Adding Line {dbContent.Title} from Code");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText,
                    LineParts.LineDivAndScriptWithCaption(dbContent));
            }

            return toProcess;
        }

        public static string ProcessForEmail(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Line Codes...");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = Db.Context().Result;

            foreach (var loopMatch in resultList)
            {
                var dbContent = context.LineContents.FirstOrDefault(x => x.ContentId == loopMatch.contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"For Email Subbing Line Map for Link {dbContent.Title} from Code");

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(),
                        UserSettingsSingleton.CurrentSettings().LinePageUrl(dbContent), "line-page-link");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
            }

            return toProcess;
        }
    }
}