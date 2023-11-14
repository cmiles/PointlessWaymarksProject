using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.S3Uploads;

[NotifyPropertyChanged]
public partial class S3UploadsUploadBatch
{
    public S3UploadsUploadBatch()
    {
        CancelCommand = new RelayCommand(() => { Cancellation?.Cancel(); }, () => Cancellation != null);
        PropertyChanged += OnPropertyChanged;
    }

    public RelayCommand? CancelCommand { get; set; }
    public CancellationTokenSource? Cancellation { get; set; }
    public bool Completed { get; set; }
    public decimal CompletedItemPercent { get; set; }
    public decimal CompletedSizePercent { get; set; }
    public S3UploadsItem? CurrentUpload { get; set; }
    public int ErrorItemCount { get; set; }
    public long ErrorSize { get; set; }
    public bool HasErrors { get; set; }
    public ObservableCollection<S3UploadsItem>? Items { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalItemCount { get; set; }
    public long TotalUploadSize { get; set; }
    public int UploadedItemCount { get; set; }
    public long UploadedSize { get; set; }
    public bool Uploading { get; set; }

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

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ErrorItemCount))
            HasErrors = ErrorItemCount > 0;
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

            if (TotalUploadSize > 0) CompletedSizePercent = (UploadedSize + ErrorSize) / (decimal)TotalUploadSize;
            if (TotalItemCount > 0)
                CompletedItemPercent = (UploadedItemCount + ErrorItemCount) / (decimal)TotalItemCount;
        }

        Uploading = false;
        Completed = true;
        Status = "Complete";
    }
}