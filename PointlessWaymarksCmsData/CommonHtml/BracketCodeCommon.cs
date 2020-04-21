using System;
using System.Text.RegularExpressions;
using Markdig;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodeCommon
    {
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
            input = BracketCodePhotos.PhotoCodeProcessToFigureWithLink(input, progress);
            input = BracketCodeImages.ImageCodeProcessToFigureWithLink(input, progress);
            input = BracketCodeFiles.FileDownloadLinkCodeProcess(input, progress);
            input = BracketCodeFiles.FileLinkCodeProcess(input, progress);
            input = BracketCodePosts.PostLinkCodeProcess(input, progress);

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var markdownOut = Markdown.ToHtml(input, pipeline);

            return markdownOut;
        }

        public static string ProcessCodesForLocalDisplay(string input, IProgress<string> progress)
        {
            input = BracketCodePhotos.PhotoCodeProcessForDirectLocalAccess(input, progress);
            input = BracketCodeImages.ImageCodeProcessForDirectLocalAccess(input, progress);
            input = BracketCodeFiles.FileLinkCodeProcess(input, progress);
            input = BracketCodeFiles.FileDownloadLinkCodeProcess(input, progress);
            input = BracketCodePosts.PostLinkCodeProcess(input, progress);

            return input;
        }
    }
}