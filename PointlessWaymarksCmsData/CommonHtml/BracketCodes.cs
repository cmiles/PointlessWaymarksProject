﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PointlessWaymarksCmsData.PhotoHtml;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodes
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
            return PhotoCodeProcess(toProcess, page => page.PictureAsset.LocalDisplayPhotoImageTag().ToString());
        }

        /// <summary>
        ///     Processes {{photo guid;human_identifier}} into figure html with a link to the photo page.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static string PhotoCodeProcessToFigure(string toProcess)
        {
            return PhotoCodeProcess(toProcess, page => page.PictureAsset.LocalPictureFigureTag().ToString());
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
    }
}