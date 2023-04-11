using System.IO;
using Amazon.S3;
using Amazon.S3.Transfer;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.S3Uploads;

public partial class S3UploadsItem : ObservableObject, ISelectedTextTracker
{
    [ObservableProperty] private string _amazonObjectKey;
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
    [ObservableProperty] private S3Information _uploadS3Information;

    public S3UploadsItem(S3Information s3Info, FileInfo fileToUpload, string amazonObjectKey, string note)
    {
        _uploadS3Information = s3Info;
        BucketName = UploadS3Information.BucketName();
        _fileToUpload = fileToUpload;
        _amazonObjectKey = amazonObjectKey;
        _note = note;
    }

    public string BucketName { get; }

    public async Task StartUpload()
    {
        if (IsUploading) return;

        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        HasError = false;
        ErrorMessage = string.Empty;
        Completed = false;

        var accessKey = UploadS3Information.AccessKey();
        var secret = UploadS3Information.Secret();

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
        {
            HasError = true;
            ErrorMessage = "Aws Credentials are not entered or valid?";
            return;
        }

        if (string.IsNullOrWhiteSpace(UploadS3Information.BucketName()))
        {
            HasError = true;
            ErrorMessage = "Bucket Name is blank?";
            return;
        }


        if (string.IsNullOrWhiteSpace(UploadS3Information.BucketRegion()))
        {
            HasError = true;
            ErrorMessage = "Amazon Region is blank?";
            return;
        }

        var region = UploadS3Information.BucketRegionEndpoint();

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

        if (string.IsNullOrWhiteSpace(AmazonObjectKey))
        {
            HasError = true;
            ErrorMessage = "Amazon Key is blank?";
            return;
        }

        try
        {
            var s3Client = new AmazonS3Client(accessKey, secret, region);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = UploadS3Information.BucketName(), FilePath = FileToUpload.FullName, Key = AmazonObjectKey
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