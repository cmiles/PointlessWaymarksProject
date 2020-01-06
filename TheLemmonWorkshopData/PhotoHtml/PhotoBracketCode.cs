using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheLemmonWorkshopWpfControls;

namespace TheLemmonWorkshopData.PhotoHtml
{
    public static class PhotoBracketCode
    {
        public static string LocalMarkdownPreprocessedForSitePhotoTags(string toProcess)
        {
            return MarkdownPreprocessedForSitePhotoTags(toProcess, page => page.LocalPhotoFigureTag().ToString());
        }

        public static string MarkdownPreprocessedForSitePhotoTags(string toProcess)
        {
            return MarkdownPreprocessedForSitePhotoTags(toProcess, page => page.PhotoFigureTag().ToString());
        }

        private static string MarkdownPreprocessedForSitePhotoTags(string toProcess,
            Func<SinglePhotoPage, string> pageConversion)
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
    }
}