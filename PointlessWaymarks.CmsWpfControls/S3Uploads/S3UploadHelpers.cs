using System.IO;
using System.Windows.Shell;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.S3;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.S3Uploads;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.S3Uploads;

public static class S3UploadHelpers
{
    public static async Task GenerateChangedHtmlAndStartUpload(StatusControlContext statusContext,
        WindowIconStatus? windowStatus = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        (IsValid validUploadList, List<S3UploadRequest> uploadItems) toUpload;
        
        try
        {
            windowStatus?.AddRequest(new WindowIconStatusRequest(statusContext.StatusControlContextId,
                TaskbarItemProgressState.Indeterminate));
            await SiteGeneration.ChangedSiteContent(statusContext.ProgressTracker());
            toUpload = await S3CmsTools.FilesSinceLastUploadToUploadList(statusContext.ProgressTracker());
            
            await S3CmsTools.S3UploaderItemsToS3UploaderJsonFile(toUpload.uploadItems,
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
        
        var window = await S3UploadsWindow.CreateInstance(S3CmsTools.S3AccountInformationFromSettings(), toUpload.uploadItems,
            UserSettingsSingleton.CurrentSettings().SiteName, true);
        
        await window.PositionWindowAndShowOnUiThread();
    }
}