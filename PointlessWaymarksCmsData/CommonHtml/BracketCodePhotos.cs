using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PointlessWaymarksCmsData.PhotoHtml;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodePhotos
    {
        /// <summary>
        ///     Processes {{photo guid;human_identifier}} with a specified function - best use may be for easily building
        ///     library code.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="pageConversion"></param>
        /// <returns></returns>
        private static string PhotoCodeProcess(string toProcess, Func<SinglePhotoPage, string> pageConversion)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            var resultList = new List<(string wholeMatch, string siteGuidMatch)>();
            var regexObj = new Regex(@"{{photo (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            while (matchResult.Success)
            {
                resultList.Add((matchResult.Value, matchResult.Groups["siteGuid"].Value));
                matchResult = matchResult.NextMatch();

                var context = Db.Context().Result;

                foreach (var loopString in resultList)
                {
                    var photoContentGuid = Guid.Parse(loopString.siteGuidMatch);
                    var dbPhoto = context.PhotoContents.FirstOrDefault(x => x.ContentId == photoContentGuid);
                    if (dbPhoto == null) continue;

                    var singlePhotoInfo = new SinglePhotoPage(dbPhoto);

                    toProcess = toProcess.Replace(loopString.wholeMatch, pageConversion(singlePhotoInfo));
                }
            }

            return toProcess;
        }

        /// <summary>
        ///     This method processes a photo code for use with the CMS Gui Previews (or for another local working
        ///     program).
        /// </summary>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static string PhotoCodeProcessForDirectLocalAccess(string toProcess)
        {
            return PhotoCodeProcess(toProcess, page => page.PictureInformation.LocalPictureFigureTag().ToString());
        }

        /// <summary>
        ///     Processes {{photo guid;human_identifier}} into figure html with a link to the photo page.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static string PhotoCodeProcessToFigureWithLink(string toProcess)
        {
            return PhotoCodeProcess(toProcess,
                page => page.PictureInformation.PictureFigureWithLinkToPicturePageTag().ToString());
        }
    }
}