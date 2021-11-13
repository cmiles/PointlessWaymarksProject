using System.Text.RegularExpressions;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml
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

        public static bool ContainsSpatialBracketCodes(IContentCommon? content)
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
        public static Guid? PhotoOrImageCodeFirstIdInContent(string? toProcess)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return null;

            var regexObj = new Regex(@"{{(?:photo|image) (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            if (matchResult.Success) return Guid.Parse(matchResult.Groups["siteGuid"].Value);

            return null;
        }

        public static async Task<string> ProcessCodesForEmail(string? input, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            input = await BracketCodeFileUrl.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeFileDownloads.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeFiles.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeFileImage.ProcessForEmail(input, progress).ConfigureAwait(false);
            input = await BracketCodeGeoJsonLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeImages.ProcessForEmail(input, progress).ConfigureAwait(false);
            input = await BracketCodeImageLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeLineLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeNotes.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePhotos.ProcessForEmail(input, progress).ConfigureAwait(false);
            input = await BracketCodePhotoLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePointLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePosts.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePostImage.ProcessForEmail(input, progress).ConfigureAwait(false);
            input = BracketCodeSpecialPages.Process(input, progress);

            // 2020/12/19 These Codes produce maps on the site but aren't going to work
            // in email, the ProcessForEmail will just remove this content
            input = await BracketCodeGeoJson.ProcessForEmail(input, progress).ConfigureAwait(false);
            input = await BracketCodeLines.ProcessForEmail(input, progress).ConfigureAwait(false);
            input = BracketCodeMapComponents.ProcessForEmail(input, progress);
            input = await BracketCodePoints.ProcessForEmail(input, progress).ConfigureAwait(false);

            return input;
        }

        public static async Task<string> ProcessCodesForLocalDisplay(string input, IProgress<string>? progress = null)
        {
            input = await BracketCodeFileUrl.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeFileDownloads.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeFiles.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeFileImage.ProcessForDirectLocalAccess(input, progress).ConfigureAwait(false);
            input = await BracketCodeGeoJson.ProcessForDirectLocalAccess(input, progress).ConfigureAwait(false);
            input = await BracketCodeGeoJsonLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeImages.ProcessForDirectLocalAccess(input, progress).ConfigureAwait(false);
            input = await BracketCodeImageLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeLines.ProcessForDirectLocalAccess(input, progress).ConfigureAwait(false);
            input = await BracketCodeLineLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeNotes.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePhotos.ProcessForDirectLocalAccess(input, progress).ConfigureAwait(false);
            input = await BracketCodePhotoLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePointLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePoints.ProcessForDirectLocalAccess(input, progress).ConfigureAwait(false);
            input = await BracketCodePosts.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePostImage.ProcessForDirectLocalAccess(input, progress).ConfigureAwait(false);
            input = BracketCodeSpecialPages.Process(input, progress);

            return input;
        }

        public static async Task<string> ProcessCodesForSite(string input, IProgress<string>? progress = null)
        {
            input = await BracketCodeFileUrl.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeFileDownloads.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeFiles.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeFileImage.ProcessToFigureWithLink(input, progress).ConfigureAwait(false);
            input = await BracketCodeGeoJson.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeGeoJsonLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeImages.ProcessToFigureWithLink(input, progress).ConfigureAwait(false);
            input = await BracketCodeImageLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeLines.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeLineLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeMapComponents.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodeNotes.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePhotos.ProcessToFigureWithLink(input, progress).ConfigureAwait(false);
            input = await BracketCodePhotoLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePoints.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePointLinks.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePosts.Process(input, progress).ConfigureAwait(false);
            input = await BracketCodePostImage.ProcessToFigureWithLink(input, progress).ConfigureAwait(false);
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