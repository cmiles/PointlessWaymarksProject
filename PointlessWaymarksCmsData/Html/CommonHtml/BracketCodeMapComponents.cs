using System;
using System.Collections.Generic;
using System.Linq;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.MapComponentData;

namespace PointlessWaymarksCmsData.Html.CommonHtml
{
    public static class BracketCodeMapComponents
    {
        public const string BracketCodeToken = "mapcomponent";

        public static string Create(MapComponent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static List<MapComponent> DbContentFromBracketCodes(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<MapComponent>();

            var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

            var returnList = new List<MapComponent>();

            if (!guidList.Any()) return returnList;

            var context = Db.Context().Result;

            foreach (var loopGuid in guidList)
            {
                var dbContent = context.MapComponents.FirstOrDefault(x => x.ContentId == loopGuid);
                if (dbContent == null) continue;

                progress?.Report($"MapComponent Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static string Process(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for MapComponent Link Codes");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = Db.Context().Result;

            foreach (var loopMatch in resultList)
            {
                var dbContent = context.MapComponents.FirstOrDefault(x => x.ContentId == loopMatch.contentGuid);
                if (dbContent == null) continue;

                progress?.Report($"Adding mapComponent {dbContent.Title} from Code");

                toProcess = toProcess.Replace(loopMatch.bracketCodeText, MapParts.MapDivAndScript(dbContent));
            }

            return toProcess;
        }
    }
}