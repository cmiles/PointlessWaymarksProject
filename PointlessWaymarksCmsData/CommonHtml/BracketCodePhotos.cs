using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.PhotoHtml;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class BracketCodePhotos
    {
        public static List<PhotoContent> DbContentFromBracketCodes(string toProcess, IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<PhotoContent>();

            progress?.Report("Searching for Photo Codes...");

            var resultList = new List<Guid>();
            var regexObj = new Regex(@"{{photo (?<siteGuid>[\dA-Za-z-]*);[^}]*}}", RegexOptions.Singleline);
            var matchResult = regexObj.Match(toProcess);
            while (matchResult.Success)
            {
                resultList.Add(Guid.Parse(matchResult.Groups["siteGuid"].Value));
                matchResult = matchResult.NextMatch();
            }

            resultList = resultList.Distinct().ToList();

            var returnList = new List<PhotoContent>();

            foreach (var loopGuid in resultList)
            {
                var context = Db.Context().Result;

                var dbContent = context.PhotoContents.FirstOrDefault(x => x.ContentId == loopGuid);
                if (dbContent == null) continue;

                progress?.Report($"Photo Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        public static string PhotoBracketCode(PhotoContent content)
        {
            return $@"{{photo {content.ContentId}; {content.Title}}}";
        }


        /// <summary>
        ///     Processes {{photo guid;human_identifier}} with a specified function - best use may be for easily building
        ///     library code.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="pageConversion"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        private static string PhotoCodeProcess(string toProcess, Func<SinglePhotoPage, string> pageConversion,
            IProgress<string> progress)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Photo Codes...");

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

                    progress?.Report($"Photo Code - Adding {dbPhoto.Title}");
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
        /// <param name="progress"></param>
        /// <returns></returns>
        public static string PhotoCodeProcessForDirectLocalAccess(string toProcess, IProgress<string> progress)
        {
            return PhotoCodeProcess(toProcess, page => page.PictureInformation.LocalPictureFigureTag().ToString(),
                progress);
        }

        /// <summary>
        ///     Processes {{photo guid;human_identifier}} into figure html with a link to the photo page.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static string PhotoCodeProcessToFigureWithLink(string toProcess, IProgress<string> progress)
        {
            return PhotoCodeProcess(toProcess,
                page => page.PictureInformation.PictureFigureWithLinkToPicturePageTag("100vw").ToString(), progress);
        }
    }
}