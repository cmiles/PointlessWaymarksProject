using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PointlessWaymarksCmsData.ImageHtml;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodeImages
    {
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
                page => page.PictureInformation.PictureFigureWithLinkToPicturePageTag().ToString(), progress);
        }
    }
}