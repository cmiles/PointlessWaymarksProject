#nullable enable
using System.IO;
using Amazon.S3;
using Amazon.S3.Transfer;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.Aws;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.S3Uploads;

public partial class S3UploadsItem : ObservableObject, ISelectedTextTracker
{
    [ObservableProperty] private string _amazonObjectKey;
    [ObservableProperty] private string _bucketName;
    [ObservableProperty] private string _bucketRegion;
    [ObservableProperty] private bool _completed;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _fileNoLongerExistsOnDisk;
    [ObservableProperty] private FileInfo _fileToUpload;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isUploading;
    [ObservableProperty] private string _note;
    [ObservableProperty] private bool _queued;
    [ObservableProperty] private CurrentSelectedTextTracker _selectedTextTracker = new();
    [ObservableProperty] private string _status = string.Empty;

    public S3UploadsItem(FileInfo fileToUpload, string amazonObjectKey, string bucket, string region, string note)
    {
        _fileToUpload = fileToUpload;
        _amazonObjectKey = amazonObjectKey;
        _bucketName = bucket;
        _note = note;
        _bucketRegion = region;
    }

    public async Task StartUpload()
    {
        if (IsUploading) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        HasError = false;
        ErrorMessage = string.Empty;
        Completed = false;

        if (string.IsNullOrWhiteSpace(BucketName))
        {
            HasError = true;
            ErrorMessage = "Bucket Name is blank?";
            return;
        }

        if (string.IsNullOrWhiteSpace(AmazonObjectKey))
        {
            HasError = true;
            ErrorMessage = "Amazon Key is blank?";
            return;
        }

        if (string.IsNullOrWhiteSpace(BucketRegion))
        {
            HasError = true;
            ErrorMessage = "Amazon Region is blank?";
            return;
        }

        var region = UserSettingsSingleton.CurrentSettings().SiteS3BucketEndpoint();

        if (region == null)
        {
            HasError = true;
            ErrorMessage = "Amazon Region is null?";
            return;
        }

        FileToUpload.Refresh();

        if (!FileToUpload.Exists)
        {
            HasError = true;
            ErrorMessage = $"File to Upload {FileToUpload.FullName} does not exist?";
            FileNoLongerExistsOnDisk = true;
            return;
        }

        var (accessKey, secret) = AwsCredentials.GetAwsSiteCredentials();

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
        {
            HasError = true;
            ErrorMessage = "Aws Credentials are not entered or valid?";
            return;
        }

        try
        {
            var s3Client = new AmazonS3Client(accessKey, secret, region);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = BucketName, FilePath = FileToUpload.FullName, Key = AmazonObjectKey
            };

            uploadRequest.UploadProgressEvent += UploadRequestOnUploadProgressEvent;

            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            Completed = true;
            IsUploading = false;
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
            HasError = true;
            IsUploading = false;
        }
    }

    private void UploadRequestOnUploadProgressEvent(object? sender, UploadProgressArgs e)
    {
        Status = $"{e.PercentDone}% Done, {e.TransferredBytes:N0} Transferred of {e.TotalBytes:N0}";
    }
}