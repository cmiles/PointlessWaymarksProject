using System.IO;
using Amazon.S3.Transfer;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.S3Uploads;

[NotifyPropertyChanged]
public partial class S3UploadsItem : ISelectedTextTracker
{
    public S3UploadsItem(IS3AccountInformation s3Info, FileInfo fileToUpload, string amazonObjectKey, string note)
    {
        UploadS3Information = s3Info;
        BucketName = UploadS3Information.BucketName();
        FileToUpload = fileToUpload;
        AmazonObjectKey = amazonObjectKey;
        Note = note;
    }
    
    public string AmazonObjectKey { get; set; }
    public string BucketName { get; }
    public bool Completed { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public bool FileNoLongerExistsOnDisk { get; set; }
    public FileInfo FileToUpload { get; set; }
    public bool HasError { get; set; }
    public bool IsUploading { get; set; }
    public string Note { get; set; }
    public bool Queued { get; set; }
    public string Status { get; set; } = string.Empty;
    public IS3AccountInformation UploadS3Information { get; set; }
    public CurrentSelectedTextTracker SelectedTextTracker { get; set; } = new();
    
    public async Task StartUpload()
    {
        if (IsUploading) return;
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        HasError = false;
        ErrorMessage = string.Empty;
        Completed = false;
        
        var isAmazon = UploadS3Information.S3Provider() == S3Providers.Amazon;
        
        var accessKey = UploadS3Information.AccessKey();
        var secret = UploadS3Information.Secret();
        
        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
        {
            HasError = true;
            ErrorMessage = "S3 Credentials are not entered or valid?";
            return;
        }

        if (string.IsNullOrWhiteSpace(UploadS3Information.ServiceUrl()))
        {
            HasError = true;
            ErrorMessage = "S3 Service URL is blank?";
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
            var s3Client = UploadS3Information.S3Client();
            
            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = UploadS3Information.BucketName(), FilePath = FileToUpload.FullName, Key = AmazonObjectKey
            };
            
            if (UploadS3Information.S3Provider() == S3Providers.Cloudflare) uploadRequest.DisablePayloadSigning = true;
            
            uploadRequest.Metadata.Add("LastWriteTime", FileToUpload.LastWriteTimeUtc.ToString("O"));
            uploadRequest.Metadata.Add("FileSystemHash", FileToUpload.CalculateMD5());
            
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