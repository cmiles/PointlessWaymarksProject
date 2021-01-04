using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.CommonHtml
{
    public static class BracketCodeCommon
    {
        /// <summary>
        ///     Extracts a list of Bracket Code ContentIds from the string.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static List<Guid> BracketCodeContentIds(string toProcess)
        {
            var returnList = new List<Guid>();

            if (string.IsNullOrWhiteSpace(toProcess)) return returnList;

            var regexObj = new Regex(@"{{[a-zA-Z-]*[ ]*(?<siteGuid>[\dA-Za-z-]*);[^}]*}}");
            var matchResult = regexObj.Match(toProcess);
            while (matchResult.Success)
            {
                if (Guid.TryParse(matchResult.Groups["siteGuid"].Value, out var toAdd)) returnList.Add(toAdd);

                matchResult = matchResult.NextMatch();
            }

            return returnList;
        }

        public static bool ContainsSpatialBracketCodes(string toProcess)
        {
            var codeMatches = new List<string>
            {
                $"{{{{{BracketCodePoints.BracketCodeToken}",
                $"{{{{{BracketCodeGeoJson.BracketCodeToken}",
                $"{{{{{BracketCodeLines.BracketCodeToken}",
                $"{{{{{BracketCodeMapComponents.BracketCodeToken}"
            };

            return codeMatches.Any(toProcess.Contains);
        }

        public static bool ContainsSpatialBracketCodes(IContentCommon content)
        {
            if (content == null) return false;

            var toSearch = string.Empty;

            toSearch += content.BodyContent + content.Summary;

            if (content is IUpdateNotes updateContent) toSearch += updateContent.UpdateNotes;

            if (string.IsNullOrWhiteSpace(toSearch)) return false;

            return ContainsSpatialBracketCodes(toSearch);
        }

        /// <summary>
        ///     Returns Bracket Code Information from a string
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="bracketCodeToken"></param>
        /// <returns></returns>
        public static List<(string bracketCodeText, Guid contentGuid, string displayText)> ContentBracketCodeMatches(
            string toProcess, string bracketCodeToken)
        {
            var resultList = new List<(string bracketCodeText, Guid contentGuid, string displayText)>();

            if (string.IsNullOrWhiteSpace(toProcess)) return resultList;

            var withTextMatch =
                new Regex(
                    $@"{{{{{bracketCodeToken} (?<siteGuid>[\dA-Za-z-]*);\s*[Tt]ext (?<displayText>[^}};]*);[^}}]*}}}}",
                    RegexOptions.Singleline);
            var noTextMatch = withTextMatch.Match(toProcess);
            while (noTextMatch.Success)
            {
                Guid.TryParse(noTextMatch.Groups["siteGuid"].Value, out var parsedContentId);
                resultList.Add((noTextMatch.Value, parsedContentId, noTextMatch.Groups["displayText"].Value));
                noTextMatch = noTextMatch.NextMatch();
            }

            //Remove the more specific pattern matches before processing the less specific matches,
            //as currently written there are patterns that can match both.
            foreach (var loopResultList in resultList)
                toProcess = toProcess.Replace(loopResultList.bracketCodeText, string.Empty);

            var regexObj = new Regex($@"{{{{{bracketCodeToken} (?<siteGuid>[\dA-Za-z-]*);[^}}]*}}}}",
                RegexOptions.Singleline);
            var textMatch = regexObj.Match(toProcess);
            while (textMatch.Success)
            {
                Guid.TryParse(textMatch.Groups["siteGuid"].Value, out var parsedContentId);
                resultList.Add((textMatch.Value, parsedContentId, string.Empty));
                textMatch = textMatch.NextMatch();
            }

            return resultList;
        }

        /// <summary>
        ///     Extracts the Guid from the first {{(photo|image) guid;human_identifier}} in the string.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static Guid? PhotoOrImageCodeFirstIdInContent(string toProcess)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return null;

            var regexObj = new Regex(@"{{(?:photo|image) (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            if (matchResult.Success) return Guid.Parse(matchResult.Groups["siteGuid"].Value);

            return null;
        }

        public static string ProcessCodesForEmail(string input, IProgress<string> progress)
        {
            input = BracketCodeFileDownloads.Process(input, progress);
            input = BracketCodeFiles.Process(input, progress);
            input = BracketCodeGeoJsonLinks.Process(input, progress);
            input = BracketCodeImages.ProcessForEmail(input, progress);
            input = BracketCodeImageLinks.Process(input, progress);
            input = BracketCodeLineLinks.Process(input, progress);
            input = BracketCodeNotes.Process(input, progress);
            input = BracketCodePhotos.ProcessForEmail(input, progress);
            input = BracketCodePhotoLinks.Process(input, progress);
            input = BracketCodePointLinks.Process(input, progress);
            input = BracketCodePosts.Process(input, progress);
            input = BracketCodeSpecialPages.Process(input, progress);

            // 2020/12/19 These Codes produce maps on the site but aren't going to work
            // in email, the ProcessForEmail will just remove this content
            input = BracketCodeGeoJson.ProcessForEmail(input, progress);
            input = BracketCodeLines.ProcessForEmail(input, progress);
            input = BracketCodeMapComponents.ProcessForEmail(input, progress);
            input = BracketCodePoints.ProcessForEmail(input, progress);

            return input;
        }

        public static string ProcessCodesForLocalDisplay(string input, IProgress<string> progress)
        {
            input = BracketCodeFileDownloads.Process(input, progress);
            input = BracketCodeFiles.Process(input, progress);
            input = BracketCodeGeoJson.Process(input, progress);
            input = BracketCodeGeoJsonLinks.Process(input, progress);
            input = BracketCodeImages.ProcessForDirectLocalAccess(input, progress);
            input = BracketCodeImageLinks.Process(input, progress);
            input = BracketCodeLines.Process(input, progress);
            input = BracketCodeLineLinks.Process(input, progress);
            input = BracketCodeNotes.Process(input, progress);
            input = BracketCodePhotos.ProcessForDirectLocalAccess(input, progress);
            input = BracketCodePhotoLinks.Process(input, progress);
            input = BracketCodePosts.Process(input, progress);
            input = BracketCodePointLinks.Process(input, progress);
            input = BracketCodePoints.Process(input, progress);
            input = BracketCodeSpecialPages.Process(input, progress);

            return input;
        }

        public static string ProcessCodesForSite(string input, IProgress<string> progress)
        {
            input = BracketCodeFileDownloads.Process(input, progress);
            input = BracketCodeFiles.Process(input, progress);
            input = BracketCodeGeoJson.Process(input, progress);
            input = BracketCodeGeoJsonLinks.Process(input, progress);
            input = BracketCodeImages.ProcessToFigureWithLink(input, progress);
            input = BracketCodeImageLinks.Process(input, progress);
            input = BracketCodeLines.Process(input, progress);
            input = BracketCodeLineLinks.Process(input, progress);
            input = BracketCodeMapComponents.Process(input, progress);
            input = BracketCodeNotes.Process(input, progress);
            input = BracketCodePhotos.ProcessToFigureWithLink(input, progress);
            input = BracketCodePhotoLinks.Process(input, progress);
            input = BracketCodePoints.Process(input, progress);
            input = BracketCodePointLinks.Process(input, progress);
            input = BracketCodePosts.Process(input, progress);
            input = BracketCodeSpecialPages.Process(input, progress);

            return input;
        }

        /// <summary>
        ///     Returns Bracket Code Information from a string
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="bracketCodeToken"></param>
        /// <returns></returns>
        public static List<(string bracketCodeText, string displayText)> SpecialPageBracketCodeMatches(string toProcess,
            string bracketCodeToken)
        {
            var resultList = new List<(string bracketCodeText, string displayText)>();

            if (string.IsNullOrWhiteSpace(toProcess)) return resultList;

            var withTextMatch = new Regex($@"{{{{{bracketCodeToken};\s*[Tt]ext (?<displayText>[^}};]*);[^}}]*}}}}",
                RegexOptions.Singleline);
            var noTextMatch = withTextMatch.Match(toProcess);
            while (noTextMatch.Success)
            {
                resultList.Add((noTextMatch.Value, noTextMatch.Groups["displayText"].Value));
                noTextMatch = noTextMatch.NextMatch();
            }

            //Remove the more specific pattern matches before processing the less specific matches,
            //as currently written there are patterns that can match both.
            foreach (var loopResultList in resultList)
                toProcess = toProcess.Replace(loopResultList.bracketCodeText, string.Empty);

            var regexObj = new Regex($@"{{{{{bracketCodeToken};}}}}", RegexOptions.Singleline);
            var textMatch = regexObj.Match(toProcess);
            while (textMatch.Success)
            {
                Guid.TryParse(textMatch.Groups["siteGuid"].Value, out _);
                resultList.Add((textMatch.Value, string.Empty));
                textMatch = textMatch.NextMatch();
            }

            return resultList;
        }
    }
}