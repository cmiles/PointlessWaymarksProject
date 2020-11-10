#nullable enable
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using JetBrains.Annotations;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.S3Uploads
{
    public class S3UploadsItem : INotifyPropertyChanged
    {
        private string _amazonObjectKey;
        private string _bucketName;
        private bool _completed;
        private string _errorMessage = string.Empty;
        private FileInfo _fileToUpload;
        private bool _hasError;
        private bool _isUploading;
        private string _note;
        private bool _queued;
        private string _status = string.Empty;

        public S3UploadsItem(FileInfo fileToUpload, string amazonObjectKey, string bucket, string note)
        {
            _fileToUpload = fileToUpload;
            _amazonObjectKey = amazonObjectKey;
            _bucketName = bucket;
            _note = note;
        }

        public string AmazonObjectKey
        {
            get => _amazonObjectKey;
            set
            {
                if (value == _amazonObjectKey) return;
                _amazonObjectKey = value;
                OnPropertyChanged();
            }
        }

        public string BucketName
        {
            get => _bucketName;
            set
            {
                if (value == _bucketName) return;
                _bucketName = value;
                OnPropertyChanged();
            }
        }

        public bool Completed
        {
            get => _completed;
            set
            {
                if (value == _completed) return;
                _completed = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (value == _errorMessage) return;
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public FileInfo FileToUpload
        {
            get => _fileToUpload;
            set
            {
                if (Equals(value, _fileToUpload)) return;
                _fileToUpload = value;
                OnPropertyChanged();
            }
        }

        public bool HasError
        {
            get => _hasError;
            set
            {
                if (value == _hasError) return;
                _hasError = value;
                OnPropertyChanged();
            }
        }

        public bool IsUploading
        {
            get => _isUploading;
            set
            {
                if (value == _isUploading) return;
                _isUploading = value;
                OnPropertyChanged();
            }
        }

        public string Note
        {
            get => _note;
            set
            {
                if (value == _note) return;
                _note = value;
                OnPropertyChanged();
            }
        }


        public bool Queued
        {
            get => _queued;
            set
            {
                if (value == _queued) return;
                _queued = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task StartUpload()
        {
            if (IsUploading) return;

            HasError = false;
            ErrorMessage = string.Empty;

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

            FileToUpload.Refresh();

            if (!FileToUpload.Exists)
            {
                HasError = true;
                ErrorMessage = $"File to Upload {FileToUpload.FullName} does not exist?";
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            var (accessKey, secret) = AwsCredentials.GetAwsSiteCredentials();

            if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
            {
                HasError = true;
                ErrorMessage = "Aws Credentials are not entered or valid?";
                return;
            }

            try
            {
                var s3Client = new AmazonS3Client(accessKey, secret);

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
            Status = $"{e.PercentDone}% Done, {e.TransferredBytes} Transferred of {e.TotalBytes}";
        }
    }
}