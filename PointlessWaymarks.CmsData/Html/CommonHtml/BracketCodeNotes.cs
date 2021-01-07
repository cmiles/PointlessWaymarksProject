using System;
using System.Collections.Generic;
using System.Linq;
using HtmlTags;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Html.CommonHtml
{
    public static class BracketCodeNotes
    {
        public const string BracketCodeToken = "notelink";

        public static string Create(NoteContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static List<NoteContent> DbContentFromBracketCodes(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<NoteContent>();

            progress?.Report("Searching for Note Codes...");

            var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

            var returnList = new List<NoteContent>();

            if (!guidList.Any()) return returnList;

            var context = Db.Context().Result;

            foreach (var loopMatch in guidList)
            {
                var dbContent = context.NoteContents.FirstOrDefault(x => x.ContentId == loopMatch);
                if (dbContent == null) continue;

                progress?.Report($"Note Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static string Process(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Note Codes...");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = Db.Context().Result;

            foreach (var loopMatch in resultList)
            {
                var dbContent = context.NoteContents.FirstOrDefault(x => x.ContentId == loopMatch.contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"Adding note link {dbContent.Title} from Code");

                var linkTag =
                    new LinkTag(
                        string.IsNullOrWhiteSpace(loopMatch.displayText)
                            ? dbContent.Title
                            : loopMatch.displayText.Trim(),
                        UserSettingsSingleton.CurrentSettings().NotePageUrl(dbContent), "note-page-link");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, linkTag.ToString());
            }

            return toProcess;
        }
    }
}