using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.PostHtml;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.Content
{
    public static class PostGenerator
    {
        public static void GenerateHtml(PostContent toGenerate, DateTime? generationVersion, IProgress<string> progress)
        {
            progress?.Report($"Post Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SinglePostPage(toGenerate) {GenerationVersion = generationVersion};

            htmlContext.WriteLocalHtml();
        }

        public static async Task<(GenerationReturn generationReturn, PostContent postContent)> SaveAndGenerateHtml(
            PostContent toSave, DateTime? generationVersion, IProgress<string> progress)
        {
            var validationReturn = await Validate(toSave);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await Db.SavePostContent(toSave);
            GenerateHtml(toSave, generationVersion, progress);
            await Export.WriteLocalDbJson(toSave);

            DataNotifications.PublishDataNotification("Post Generator", DataNotificationContentType.Post,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

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

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(postContent.UpdateNotesFormat);
            if (!updateFormatCheck.isValid)
                return await GenerationReturn.Error(updateFormatCheck.explanation, postContent.ContentId);

            return await GenerationReturn.Success("Post Content Validation Successful");
        }
    }
}