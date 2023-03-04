#nullable enable
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.S3;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.S3Uploads;

/// <summary>
///     Interaction logic for S3UploadsWindow.xaml
/// </summary>
[ObservableObject]
public partial class S3UploadsWindow
{
    [ObservableProperty] private bool _forceClose;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private S3UploadsContext? _uploadContext;
    [ObservableProperty] private WindowIconStatus? _windowStatus;

    public S3UploadsWindow(List<S3UploadRequest> toLoad, bool autoStartUpload)
    {
        InitializeComponent();

        _statusContext = new StatusControlContext();
        _windowStatus = new WindowIconStatus();

        DataContext = this;

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            UploadContext = await S3UploadsContext.CreateInstance(StatusContext, toLoad, WindowStatus);
            if (autoStartUpload) UploadContext.StatusContext.RunNonBlockingTask(UploadContext.StartAllUploads);
        });
    }

    private void S3UploadsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (_forceClose) return;

        StatusContext.RunFireAndForgetNonBlockingTask(WindowCloseOverload);
        e.Cancel = true;
    }

    public async Task WindowCloseOverload()
    {
        if (UploadContext?.UploadBatch == null || !UploadContext.UploadBatch.Uploading)
        {
            _forceClose = true;
            await ThreadSwitcher.ResumeForegroundAsync();
            Close();
        }

        var userAction = await StatusContext.ShowMessage("Running Upload...",
            "Exiting this window with an upload running could create errors on S3:",
            new List<string> { "Close Immediately", "Cancel and Close", "Return to Upload" });

        switch (userAction)
        {
            case "Close Immediately":
            {
                _forceClose = true;
                await ThreadSwitcher.ResumeForegroundAsync();
                Close();
                break;
            }
            case "Return and Cancel":
            {
                UploadContext?.UploadBatch?.Cancellation?.Cancel();
                break;
            }
        }
    }
}