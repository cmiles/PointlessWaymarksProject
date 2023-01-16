using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.VideoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CommonTools;
using Serilog;

namespace PointlessWaymarks.CmsData.Content;

public static class VideoGenerator
{
    public static async Task GenerateHtml(VideoContent toGenerate, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        progress?.Report($"Video Content - Generate HTML for {toGenerate.Title}");

        var htmlContext = new SingleVideoPage(toGenerate) { GenerationVersion = generationVersion };

        await htmlContext.WriteLocalHtml().ConfigureAwait(false);
    }


    public static async Task<(GenerationReturn generationReturn, VideoContent? VideoContent)> SaveAndGenerateHtml(
        VideoContent toSave, FileInfo selectedVideo, bool overwriteExistingVideos, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave, selectedVideo).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        toSave.OriginalFileName = selectedVideo.Name;
        await FileManagement.WriteSelectedVideoContentFileToMediaArchive(selectedVideo).ConfigureAwait(false);
        await Db.SaveVideoContent(toSave).ConfigureAwait(false);
        await WriteVideoFromMediaArchiveToLocalSite(toSave, overwriteExistingVideos).ConfigureAwait(false);
        await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteLocalDbJson(toSave, progress).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Video Generator", DataNotificationContentType.Video,
            DataNotificationUpdateType.LocalContent, new List<Guid> { toSave.ContentId });

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<GenerationReturn> Validate(VideoContent? VideoContent, FileInfo? selectedVideo)
    {
        if (VideoContent == null)
            return GenerationReturn.Error("Null Video Content submitted to Validate?");

        if (selectedVideo == null)
            return GenerationReturn.Error("No Video submitted to Validate?", VideoContent.ContentId);

        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                VideoContent.ContentId);

        var mediaArchiveCheck = UserSettingsUtilities.ValidateLocalMediaArchive();
        if (!mediaArchiveCheck.Valid)
            return GenerationReturn.Error($"Problem with Media Archive: {mediaArchiveCheck.Explanation}",
                VideoContent.ContentId);

        var (valid, explanation) =
            await CommonContentValidation.ValidateContentCommon(VideoContent).ConfigureAwait(false);
        if (!valid)
            return GenerationReturn.Error(explanation, VideoContent.ContentId);

        var (userMainImageIsValid, userMainImageExplanation) =
            await CommonContentValidation.ValidateUserMainPicture(VideoContent.UserMainPicture)
                .ConfigureAwait(false);
        if (!userMainImageIsValid)
            return GenerationReturn.Error(userMainImageExplanation, VideoContent.ContentId);

        var (isValid, s) = CommonContentValidation.ValidateUpdateContentFormat(VideoContent.UpdateNotesFormat);
        if (!isValid)
            return GenerationReturn.Error(s, VideoContent.ContentId);

        selectedVideo.Refresh();

        if (!selectedVideo.Exists)
            return GenerationReturn.Error("Selected Video doesn't exist?", VideoContent.ContentId);

        if (!FileAndFolderTools.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(selectedVideo.Name)))
            return GenerationReturn.Error("Limit Video Names to A-Z a-z - . _", VideoContent.ContentId);

        if (await (await Db.Context().ConfigureAwait(false))
            .VideoFilenameExistsInDatabase(selectedVideo.Name, VideoContent.ContentId).ConfigureAwait(false))
            return GenerationReturn.Error(
                "This Videoname already exists in the database - Video names must be unique.", VideoContent.ContentId);

        return GenerationReturn.Success("Video Content Validation Successful");
    }

    public static async Task WriteVideoFromMediaArchiveToLocalSite(VideoContent VideoContent, bool overwriteExisting)
    {
        if (string.IsNullOrWhiteSpace(VideoContent.OriginalFileName))
        {
            Log.Warning(
                $"VideoContent with a blank {nameof(VideoContent.OriginalFileName)} was submitted to WriteVideoFromMediaArchiveToLocalSite");
            return;
        }

        var userSettings = UserSettingsSingleton.CurrentSettings();

        var sourceVideo = new FileInfo(Path.Combine(userSettings.LocalMediaArchiveVideoDirectory().FullName,
            VideoContent.OriginalFileName));

        var targetVideo = new FileInfo(Path.Combine(userSettings.LocalSiteVideoContentDirectory(VideoContent).FullName,
            VideoContent.OriginalFileName));

        if (targetVideo.Exists && overwriteExisting)
        {
            targetVideo.Delete();
            targetVideo.Refresh();
        }

        if (!targetVideo.Exists) await sourceVideo.CopyToAndLog(targetVideo.FullName).ConfigureAwait(false);
    }
}