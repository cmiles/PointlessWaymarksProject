using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PointlessWaymarksCmsData.ImageHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodeImages
    {
        public static List<ImageContent> DbContentFromBracketCodes(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<ImageContent>();

            progress?.Report("Searching for Image Codes...");

            var resultList = new List<Guid>();
            var regexObj = new Regex(@"{{image (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            while (matchResult.Success)
            {
                resultList.Add(Guid.Parse(matchResult.Groups["siteGuid"].Value));
                matchResult = matchResult.NextMatch();
            }

            resultList = resultList.Distinct().ToList();

            var returnList = new List<ImageContent>();

            foreach (var loopGuid in resultList)
            {
                var context = Db.Context().Result;

                var dbContent = context.ImageContents.FirstOrDefault(x => x.ContentId == loopGuid);
                if (dbContent == null) continue;

                progress?.Report($"Image Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        /// <summary>
        ///     Processes {{image guid;human_identifier}} with a specified function - best use may be for easily building
        ///     library code.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="pageConversion"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        private static string ImageCodeProcess(string toProcess, Func<SingleImagePage, string> pageConversion,
            IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Image Codes");

            var resultList = new List<(string wholeMatch, string siteGuidMatch)>();
            var regexObj = new Regex(@"{{image (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            while (matchResult.Success)
            {
                resultList.Add((matchResult.Value, matchResult.Groups["siteGuid"].Value));
                matchResult = matchResult.NextMatch();

                var context = Db.Context().Result;

                foreach (var loopString in resultList)
                {
                    var imageContentGuid = Guid.Parse(loopString.siteGuidMatch);
                    var dbImage = context.ImageContents.FirstOrDefault(x => x.ContentId == imageContentGuid);
                    if (dbImage == null) continue;

                    progress?.Report($"Image Code for {dbImage.Title} processed");
                    var singleImageInfo = new SingleImagePage(dbImage);

                    toProcess = toProcess.Replace(loopString.wholeMatch, pageConversion(singleImageInfo));
                }
            }

            return toProcess;
        }

        /// <summary>
        ///     This method processes a image code for use with the CMS Gui Previews (or for another local working
        ///     program).
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static string ImageCodeProcessForDirectLocalAccess(string toProcess, IProgress<string> progress)
        {
            return ImageCodeProcess(toProcess, page => page.PictureInformation.LocalPictureFigureTag().ToString(),
                progress);
        }

        /// <summary>
        ///     Processes {{image guid;human_identifier}} into figure html with a link to the image page.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static string ImageCodeProcessToFigureWithLink(string toProcess, IProgress<string> progress)
        {
            return ImageCodeProcess(toProcess,
                page => page.PictureInformation.PictureFigureWithLinkToPicturePageTag("100vw").ToString(), progress);
        }
    }
}