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
        VideoContent toSave, FileInfo selectedVideo, DateTime? generationVersion,
        IProgress<string>? progress = null)
    {
        var validationReturn = await Validate(toSave, selectedVideo).ConfigureAwait(false);

        if (validationReturn.HasError) return (validationReturn, null);

        Db.DefaultPropertyCleanup(toSave);
        toSave.Tags = Db.TagListCleanup(toSave.Tags);

        toSave.OriginalFileName = selectedVideo.Name;
        await FileManagement.WriteSelectedVideoContentFileToMediaArchive(selectedVideo).ConfigureAwait(false);
        await Db.SaveVideoContent(toSave).ConfigureAwait(false);
        await WriteVideoFromMediaArchiveToLocalSiteIfNeeded(toSave).ConfigureAwait(false);
        await GenerateHtml(toSave, generationVersion, progress).ConfigureAwait(false);
        await Export.WriteVideoContentData(toSave, progress).ConfigureAwait(false);

        DataNotifications.PublishDataNotification("Video Generator", DataNotificationContentType.Video,
            DataNotificationUpdateType.LocalContent, new List<Guid> { toSave.ContentId });

        return (GenerationReturn.Success($"Saved and Generated Content And Html for {toSave.Title}"), toSave);
    }

    public static async Task<GenerationReturn> Validate(VideoContent? videoContent, FileInfo? selectedVideo)
    {
        if (videoContent == null)
            return GenerationReturn.Error("Null Video Content submitted to Validate?");

        if (selectedVideo == null)
            return GenerationReturn.Error("No Video submitted to Validate?", videoContent.ContentId);

        var rootDirectoryCheck = UserSettingsUtilities.ValidateLocalSiteRootDirectory();

        if (!rootDirectoryCheck.Valid)
            return GenerationReturn.Error($"Problem with Root Directory: {rootDirectoryCheck.Explanation}",
                videoContent.ContentId);

        var mediaArchiveCheck = UserSettingsUtilities.ValidateLocalMediaArchive();
        if (!mediaArchiveCheck.Valid)
            return GenerationReturn.Error($"Problem with Media Archive: {mediaArchiveCheck.Explanation}",
                videoContent.ContentId);

        var (valid, explanation) =
            await CommonContentValidation.ValidateContentCommon(videoContent).ConfigureAwait(false);
        if (!valid)
            return GenerationReturn.Error(explanation, videoContent.ContentId);

        var (userMainImageIsValid, userMainImageExplanation) =
            await CommonContentValidation.ValidateUserMainPicture(videoContent.UserMainPicture)
                .ConfigureAwait(false);
        if (!userMainImageIsValid)
            return GenerationReturn.Error(userMainImageExplanation, videoContent.ContentId);

        var (isValid, s) = CommonContentValidation.ValidateUpdateContentFormat(videoContent.UpdateNotesFormat);
        if (!isValid)
            return GenerationReturn.Error(s, videoContent.ContentId);

        selectedVideo.Refresh();

        if (!selectedVideo.Exists)
            return GenerationReturn.Error("Selected Video doesn't exist?", videoContent.ContentId);

        if (!FileAndFolderTools.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(selectedVideo.Name)))
            return GenerationReturn.Error("Limit Video Names to A-Z a-z - . _", videoContent.ContentId);

        if (await (await Db.Context().ConfigureAwait(false))
            .VideoFilenameExistsInDatabase(selectedVideo.Name, videoContent.ContentId).ConfigureAwait(false))
            return GenerationReturn.Error(
                "This Video Name already exists in the database - Video names must be unique.", videoContent.ContentId);

        return GenerationReturn.Success("Video Content Validation Successful");
    }

    public static async Task WriteVideoFromMediaArchiveToLocalSiteIfNeeded(VideoContent videoContent)
    {
        if (string.IsNullOrWhiteSpace(videoContent.OriginalFileName))
        {
            Log.Warning(
                $"VideoContent with a blank {nameof(videoContent.OriginalFileName)} was submitted to WriteVideoFromMediaArchiveToLocalSite");
            return;
        }

        var userSettings = UserSettingsSingleton.CurrentSettings();

        var sourceVideo = new FileInfo(Path.Combine(userSettings.LocalMediaArchiveVideoDirectory().FullName,
            videoContent.OriginalFileName));

        var targetVideo = new FileInfo(Path.Combine(userSettings.LocalSiteVideoContentDirectory(videoContent).FullName,
            videoContent.OriginalFileName));

        if (!targetVideo.Exists || sourceVideo.CalculateMD5() != targetVideo.CalculateMD5())
        {
            if (targetVideo.Exists)
            {
                targetVideo.Delete();
                targetVideo.Refresh();
            }

            await sourceVideo.CopyToAndLog(targetVideo.FullName).ConfigureAwait(false);
        }
    }
}