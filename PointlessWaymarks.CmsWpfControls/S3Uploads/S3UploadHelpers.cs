using System.IO;
using System.Windows.Shell;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.S3;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.S3Uploads;

public static class S3UploadHelpers
{
    public static async Task GenerateChangedHtmlAndStartUpload(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        (IsValid validUploadList, List<S3UploadRequest> uploadItems) toUpload;

        try
        {
            windowStatus?.AddRequest(new WindowIconStatusRequest(statusContext.StatusControlContextId,
                TaskbarItemProgressState.Indeterminate));
            await HtmlGenerationGroups.GenerateChangedToHtml(statusContext.ProgressTracker());
            toUpload = await S3Tools.FilesSinceLastUploadToUploadList(statusContext.ProgressTracker());

            await S3Tools.S3UploaderItemsToS3UploaderJsonFile(toUpload.uploadItems,
                Path.Combine(UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName,
                    $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Data.json"));

            if (!toUpload.validUploadList.Valid)
            {
                await statusContext.ShowMessageWithOkButton("Upload Failure",
                    $"Generating HTML appears to have succeeded but creating an upload failed: {toUpload.validUploadList.Explanation}");
                return;
            }
        }
        finally
        {
            windowStatus?.AddRequest(new WindowIconStatusRequest(statusContext.StatusControlContextId,
                TaskbarItemProgressState.None));
        }

        await ThreadSwitcher.ResumeForegroundAsync();
        new S3UploadsWindow(toUpload.uploadItems, true).PositionWindowAndShow();
    }
}