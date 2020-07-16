using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.PostHtml;
using PointlessWaymarksCmsData.Json;

namespace PointlessWaymarksCmsData.Content
{
    public static class PostGenerator
    {
        public static void GenerateHtml(PostContent toGenerate, IProgress<string> progress)
        {
            progress?.Report($"Post Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SinglePostPage(toGenerate);

            htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, PostContent postContent)> SaveAndGenerateHtml(
            PostContent toSave, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            await Db.SavePostContent(toSave);
            GenerateHtml(toSave, progress);
            await Export.WriteLocalDbJson(toSave);

            await DataNotifications.PublishDataNotification("Post Generator", DataNotificationContentType.Post,
                DataNotificationUpdateType.LocalContent, new List<Guid> { toSave.ContentId });

            return (await GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(PostContent postContent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Item1)
                return await GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Item2}",
                    postContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(postContent);
            if (!commonContentCheck.valid)
                return await GenerationReturn.Error(commonContentCheck.explanation, postContent.ContentId);

            if (postContent == null) return await GenerationReturn.Error("Post Content is Null?");

            return await GenerationReturn.Success("Post Content Validation Successful");
        }
    }


}
