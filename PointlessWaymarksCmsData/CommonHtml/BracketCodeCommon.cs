using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Markdig;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodeCommon
    {
        /// <summary>
        /// Returns Bracket Code Information from a string
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="bracketcodeToken"></param>
        /// <returns></returns>
        public static List<(string bracketCodeText, Guid contentGuid, string displayText)> BracketCodeMatches(
            string toProcess, string bracketcodeToken)
        {
            var resultList = new List<(string bracketCodeText, Guid contentGuid, string displayText)>();

            if (string.IsNullOrWhiteSpace(toProcess)) return resultList;

            var withTextMatch = new Regex($@"{{{{{bracketcodeToken} (?<siteGuid>[\dA-Za-z-]*);[^}}]*}}}}",
                RegexOptions.Singleline);
            var withTextMatchResult = withTextMatch.Match(toProcess);
            while (withTextMatchResult.Success)
            {
                Guid.TryParse(withTextMatchResult.Groups["siteGuid"].Value, out Guid parsedContentId);
                resultList.Add((withTextMatchResult.Value, parsedContentId,
                    withTextMatchResult.Groups["displayText"].Value));
                withTextMatchResult = withTextMatchResult.NextMatch();
            }

            var regexObj =
                new Regex(
                    $@"{{{{{bracketcodeToken} (?<siteGuid>[\dA-Za-z-]*);\s*text (?<displayText>[^}};]*);[^}}]*}}}}",
                    RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            while (matchResult.Success)
            {
                Guid.TryParse(withTextMatchResult.Groups["siteGuid"].Value, out Guid parsedContentId);
                resultList.Add((matchResult.Value, parsedContentId, string.Empty));
                matchResult = matchResult.NextMatch();
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

        public static string ProcessCodesAndMarkdownForSite(string input, IProgress<string> progress)
        {
            input = BracketCodeFileDownload.FileDownloadLinkCodeProcess(input, progress);
            input = BracketCodeFiles.FileLinkCodeProcess(input, progress);
            input = BracketCodeImages.ImageCodeProcessToFigureWithLink(input, progress);
            input = BracketCodeNotes.NoteLinkCodeProcess(input, progress);
            input = BracketCodePhotos.PhotoCodeProcessToFigureWithLink(input, progress);
            input = BracketCodePosts.PostLinkCodeProcess(input, progress);

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var markdownOut = Markdown.ToHtml(input, pipeline);

            return markdownOut;
        }

        public static string ProcessCodesForLocalDisplay(string input, IProgress<string> progress)
        {
            input = BracketCodeFileDownload.FileDownloadLinkCodeProcess(input, progress);
            input = BracketCodeFiles.FileLinkCodeProcess(input, progress);
            input = BracketCodeImages.ImageCodeProcessForDirectLocalAccess(input, progress);
            input = BracketCodeNotes.NoteLinkCodeProcess(input, progress);
            input = BracketCodePhotos.PhotoCodeProcessForDirectLocalAccess(input, progress);
            input = BracketCodePosts.PostLinkCodeProcess(input, progress);

            return input;
        }

        public static string ProcessCodesForEmail(string input, IProgress<string> progress)
        {
            input = BracketCodeFileDownload.FileDownloadLinkCodeProcess(input, progress);
            input = BracketCodeFiles.FileLinkCodeProcess(input, progress);
            input = BracketCodeImages.ImageCodeProcessForEmail(input, progress);
            input = BracketCodeNotes.NoteLinkCodeProcess(input, progress);
            input = BracketCodePhotos.PhotoCodeProcessForEmail(input, progress);
            input = BracketCodePosts.PostLinkCodeProcess(input, progress);

            return input;
        }
    }
}