﻿#nullable enable
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.S3Uploads;

[ObservableObject]
public partial class S3UploadsUploadBatch
{
    [ObservableProperty] private Command? _cancelCommand;
    [ObservableProperty] private CancellationTokenSource? _cancellation;
    [ObservableProperty] private bool _completed;
    [ObservableProperty] private decimal _completedItemPercent;
    [ObservableProperty] private decimal _completedSizePercent;
    [ObservableProperty] private S3UploadsItem? _currentUpload;
    [ObservableProperty] private int _errorItemCount;
    [ObservableProperty] private long _errorSize;
    [ObservableProperty] private ObservableCollection<S3UploadsItem>? _items;
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private int _totalItemCount;
    [ObservableProperty] private long _totalUploadSize;
    [ObservableProperty] private int _uploadedItemCount;
    [ObservableProperty] private long _uploadedSize;
    [ObservableProperty] private bool _uploading;

    public S3UploadsUploadBatch()
    {
        CancelCommand = new Command(() => { Cancellation?.Cancel(); }, () => Cancellation != null);
    }

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