#nullable enable
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.S3Uploads;

/// <summary>
///     Interaction logic for S3UploadsWindow.xaml
/// </summary>
[ObservableObject]
#pragma warning disable MVVMTK0033
public partial class S3UploadsWindow
#pragma warning restore MVVMTK0033
{
    [ObservableProperty] private bool _forceClose;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private S3UploadsContext? _uploadContext;
    [ObservableProperty] private WindowIconStatus? _windowStatus;

    public S3UploadsWindow(S3AccountInformation s3Info, List<S3UploadRequest> toLoad, bool autoStartUpload)
    {
        InitializeComponent();

        _statusContext = new StatusControlContext();
        _windowStatus = new WindowIconStatus();

        DataContext = this;

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            UploadContext = await S3UploadsContext.CreateInstance(StatusContext, s3Info, toLoad, WindowStatus);
            if (autoStartUpload) UploadContext.StatusContext.RunNonBlockingTask(UploadContext.StartAllUploads);
        });
    }

    private void S3UploadsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (ForceClose) return;

        StatusContext.RunFireAndForgetNonBlockingTask(WindowCloseOverload);
        e.Cancel = true;
    }

    public async Task WindowCloseOverload()
    {
        if (UploadContext?.UploadBatch is not { Uploading: true })
        {
            ForceClose = true;
            await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();
            Close();
        }

        var userAction = await StatusContext.ShowMessage("Running Upload...",
            "Exiting this window with an upload running could create errors on S3:",
            new List<string> { "Close Immediately", "Cancel and Close", "Return to Upload" });

        switch (userAction)
        {
            case "Close Immediately":
            {
                ForceClose = true;
                await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();
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