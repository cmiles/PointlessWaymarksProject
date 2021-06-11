using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.CommonHtml
{
    public static class BracketCodePostImage
    {
        public const string BracketCodeToken = "postimagelink";

        public static string Create(PostContent content)
        {
            return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
        }

        public static async Task<List<PostContent>> DbContentFromBracketCodes(string toProcess,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return new List<PostContent>();

            progress?.Report("Searching for Post Content Codes...");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
                .Select(x => x.contentGuid).Distinct().ToList();

            var returnList = new List<PostContent>();

            if (!resultList.Any()) return returnList;

            foreach (var loopGuid in resultList)
            {
                var context = await Db.Context();

                var dbContent = await context.PostContents.FirstOrDefaultAsync(x => x.ContentId == loopGuid);
                if (dbContent == null) continue;

                progress?.Report($"Post Image Code - Adding DbContent For {dbContent.Title}");

                returnList.Add(dbContent);
            }

            return returnList;
        }

        /// <summary>
        ///     Processes {{post guid;human_identifier}} with a specified function - best use may be for easily building
        ///     library code.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="pageConversion"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        private static async Task<string> Process(string toProcess,
            Func<(PictureSiteInformation pictureInfo, string linkUrl), string> pageConversion,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            progress?.Report("Searching for Post Image Link Codes");

            var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

            if (!resultList.Any()) return toProcess;

            var context = await Db.Context();

            foreach (var loopMatch in resultList)
            {
                var dbPost = await context.PostContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid);

                if (dbPost == null) continue;
                if (dbPost.MainPicture == null)
                {
                    progress?.Report(
                        $"Post Image Link without Main Image - converting to post - Post: {dbPost.Title}");

                    loopMatch.bracketCodeText.Replace(BracketCodeToken, BracketCodePosts.BracketCodeToken,
                        StringComparison.OrdinalIgnoreCase);

                    await BracketCodePosts.Process(toProcess);

                    continue;
                }

                var dbPicture = new PictureSiteInformation(dbPost.MainPicture.Value);

                if (dbPicture.Pictures == null)
                {
                    progress?.Report(
                        $"Post Image Link with Null PictureSiteInformation - converting to post - Post: {dbPost.Title}");

                    loopMatch.bracketCodeText.Replace(BracketCodeToken, BracketCodePosts.BracketCodeToken,
                        StringComparison.OrdinalIgnoreCase);

                    await BracketCodePosts.Process(toProcess);

                    continue;
                }

                toProcess = toProcess.Replace(loopMatch.bracketCodeText,
                    pageConversion((dbPicture, UserSettingsSingleton.CurrentSettings().PostPageUrl(dbPost))));

                progress?.Report($"Post Image Link {dbPost.Title} processed");
            }

            return toProcess;
        }

        /// <summary>
        ///     This method processes a postimagelink code for use with the CMS Gui Previews (or for another local working
        ///     program).
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static async Task<string> ProcessForDirectLocalAccess(string toProcess,
            IProgress<string>? progress = null)
        {
            return await Process(toProcess, pictureInfo => pictureInfo.pictureInfo.LocalPictureFigureTag().ToString(),
                progress);
        }

        /// <summary>
        ///     This method processes a postimagelink code for use in email.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static async Task<string> ProcessForEmail(string toProcess, IProgress<string>? progress = null)
        {
            return await Process(toProcess, pictureInfo => pictureInfo.pictureInfo.EmailPictureTableTag().ToString(),
                progress);
        }

        /// <summary>
        ///     Processes {{image guid;human_identifier}} into figure html with a link to the post page.
        /// </summary>
        /// <param name="toProcess"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static async Task<string> ProcessToFigureWithLink(string toProcess, IProgress<string>? progress = null)
        {
            return await Process(toProcess,
                pictureInfo => pictureInfo.pictureInfo.PictureFigureWithCaptionAndLinkTag("100vw", pictureInfo.linkUrl)
                    .ToString(), progress);
        }
    }
}