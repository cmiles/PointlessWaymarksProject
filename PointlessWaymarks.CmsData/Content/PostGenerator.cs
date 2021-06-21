using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PointlessWaymarks.CmsData.ContentHtml.PostHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsData.Content
{
    public static class PostGenerator
    {
        public static async Task GenerateHtml(PostContent toGenerate, DateTime? generationVersion,
            IProgress<string>? progress = null)
        {
            progress?.Report($"Post Content - Generate HTML for {toGenerate.Title}");

            var htmlContext = new SinglePostPage(toGenerate) {GenerationVersion = generationVersion};

            await htmlContext.WriteLocalHtml().ConfigureAwait(false);
        }

        public static async Task<(GenerationReturn generationReturn, PostContent? postContent)> SaveAndGenerateHtml(
            PostContent toSave, DateTime? generationVersion, IProgress<string>? progress = null)
        {
            var validationReturn = await Validate(toSave).ConfigureAwait(false);

            if (validationReturn.HasError) return (validationReturn, null);

            Db.DefaultPropertyCleanup(toSave);
            toSave.Tags = Db.TagListCleanup(toSave.Tags);

            await Db.SavePostContent(toSave).ConfigureAwait(false);
            await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
            await Export.WriteLocalDbJson(toSave).ConfigureAwait(false);

            DataNotifications.PublishDataNotification("Post Generator", DataNotificationContentType.Post,
                DataNotificationUpdateType.LocalContent, new List<Guid> {toSave.ContentId});

            return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
        }

        public static async Task<GenerationReturn> Validate(PostContent postContent)
        {
            var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

            if (!rootDirectoryCheck.Valid)
                return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                    postContent.ContentId);

            var commonContentCheck = await CommonContentValidation.ValidateContentCommon(postContent).ConfigureAwait(false);
            if (!commonContentCheck.Valid)
                return GenerationReturn.Error(commonContentCheck.Explanation, postContent.ContentId);

            var updateFormatCheck = CommonContentValidation.ValidateUpdateContentFormat(postContent.UpdateNotesFormat);
            if (!updateFormatCheck.Valid)
                return GenerationReturn.Error(updateFormatCheck.Explanation, postContent.ContentId);

            return GenerationReturn.Success("Post Content Validation Successful");
        }
    }
}