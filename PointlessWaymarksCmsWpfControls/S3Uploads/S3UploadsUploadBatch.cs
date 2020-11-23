#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarksCmsWpfControls.S3Uploads
{
    public class S3UploadsUploadBatch : INotifyPropertyChanged
    {
        private Command? _cancelCommand;
        private CancellationTokenSource? _cancellation;
        private bool _completed;
        private decimal _completedItemPercent;
        private decimal _completedSizePercent;
        private S3UploadsItem? _currentUpload;
        private int _errorItemCount;
        private long _errorSize;
        private ObservableCollection<S3UploadsItem>? _items;
        private string _status = string.Empty;
        private int _totalItemCount;
        private long _totalUploadSize;
        private int _uploadedItemCount;
        private long _uploadedSize;
        private bool _uploading;

        public S3UploadsUploadBatch()
        {
            CancelCommand = new Command(() => { Cancellation?.Cancel(); }, () => Cancellation != null);
        }

        public Command? CancelCommand
        {
            get => _cancelCommand;
            set
            {
                if (Equals(value, _cancelCommand)) return;
                _cancelCommand = value;
                OnPropertyChanged();
            }
        }

        public CancellationTokenSource? Cancellation
        {
            get => _cancellation;
            set
            {
                if (Equals(value, _cancellation)) return;
                _cancellation = value;
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

        public decimal CompletedItemPercent
        {
            get => _completedItemPercent;
            set
            {
                if (value == _completedItemPercent) return;
                _completedItemPercent = value;
                OnPropertyChanged();
            }
        }

        public decimal CompletedSizePercent
        {
            get => _completedSizePercent;
            set
            {
                if (value == _completedSizePercent) return;
                _completedSizePercent = value;
                OnPropertyChanged();
            }
        }

        public S3UploadsItem? CurrentUpload
        {
            get => _currentUpload;
            set
            {
                if (Equals(value, _currentUpload)) return;
                _currentUpload = value;
                OnPropertyChanged();
            }
        }

        public int ErrorItemCount
        {
            get => _errorItemCount;
            set
            {
                if (value == _errorItemCount) return;
                _errorItemCount = value;
                OnPropertyChanged();
            }
        }

        public long ErrorSize
        {
            get => _errorSize;
            set
            {
                if (value == _errorSize) return;
                _errorSize = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<S3UploadsItem>? Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
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

        public int TotalItemCount
        {
            get => _totalItemCount;
            set
            {
                if (value == _totalItemCount) return;
                _totalItemCount = value;
                OnPropertyChanged();
            }
        }

        public long TotalUploadSize
        {
            get => _totalUploadSize;
            set
            {
                if (value == _totalUploadSize) return;
                _totalUploadSize = value;
                OnPropertyChanged();
            }
        }

        public int UploadedItemCount
        {
            get => _uploadedItemCount;
            set
            {
                if (value == _uploadedItemCount) return;
                _uploadedItemCount = value;
                OnPropertyChanged();
            }
        }

        public long UploadedSize
        {
            get => _uploadedSize;
            set
            {
                if (value == _uploadedSize) return;
                _uploadedSize = value;
                OnPropertyChanged();
            }
        }

        public bool Uploading
        {
            get => _uploading;
            set
            {
                if (value == _uploading) return;
                _uploading = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static async Task<S3UploadsUploadBatch> CreateInstance(List<S3UploadsItem> toUpload)
        {
            var newContext = new S3UploadsUploadBatch();
            await newContext.LoadData(toUpload);
            return newContext;
        }

        private async Task LoadData(List<S3UploadsItem> toUpload)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            toUpload.ForEach(x => x.Queued = true);

            TotalItemCount = toUpload.Count;
            TotalUploadSize = toUpload.Sum(x => x.FileToUpload.Exists ? x.FileToUpload.Length : 0);

            await ThreadSwitcher.ResumeForegroundAsync();

            if (Items == null)
            {
                Items = new ObservableCollection<S3UploadsItem>(toUpload);
            }
            else
            {
                Items.Clear();
                toUpload.ForEach(x => Items.Add(x));
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task StartUploadBatch()
        {
            if (Items == null)
            {
                Completed = true;
                Status = "Complete - Nothing to Process?";
                return;
            }

            Uploading = true;
            Status = "Uploading";

            Cancellation = new CancellationTokenSource();
            var cancelToken = Cancellation.Token;

            foreach (var loopSelected in Items.ToList())
            {
                if (cancelToken.IsCancellationRequested)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    foreach (var loopItems in Items) loopItems.Queued = false;

                    await ThreadSwitcher.ResumeBackgroundAsync();

                    Status = "Canceled";
                    Uploading = false;

                    throw new TaskCanceledException(
                        $"Canceled S3 Upload Batch - Uploaded Items {UploadedItemCount}, Error Items {ErrorItemCount}, Total Items {TotalItemCount}");
                }

                try
                {
                    CurrentUpload = loopSelected;
                    await loopSelected.StartUpload();
                }
                catch (Exception e)
                {
                    loopSelected.HasError = true;
                    loopSelected.ErrorMessage = e.Message;
                }
                finally
                {
                    CurrentUpload = null;
                }

                loopSelected.Queued = false;

                if (loopSelected.HasError)
                {
                    ErrorItemCount++;
                    if (!loopSelected.FileNoLongerExistsOnDisk) ErrorSize += loopSelected.FileToUpload.Length;
                }
                else
                {
                    UploadedItemCount++;
                    UploadedSize += loopSelected.FileToUpload.Length;
                }

                CompletedSizePercent = (UploadedSize + ErrorSize) / (decimal) TotalUploadSize;
                CompletedItemPercent = (UploadedItemCount + ErrorItemCount) / (decimal) TotalItemCount;
            }

            Uploading = false;
            Completed = true;
            Status = "Complete";
        }
    }
}