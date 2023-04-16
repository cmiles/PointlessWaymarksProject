﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.S3Uploads;

public partial class S3UploadsContext : ObservableObject
{
    [ObservableProperty] private RelayCommand _clearCompletedUploadBatch;
    [ObservableProperty] private RelayCommand _clearUploadedCommand;
    [ObservableProperty] private ObservableCollection<S3UploadsItem>? _items;
    [ObservableProperty] private ContentListSelected<S3UploadsItem>? _listSelection;
    [ObservableProperty] private RelayCommand<S3UploadsItem> _openLocalFileInExplorerCommand;
    [ObservableProperty] private WindowIconStatus? _osStatusIndicator;
    [ObservableProperty] private RelayCommand _removeSelectedItemsCommand;
    [ObservableProperty] private RelayCommand _saveAllToUploadJsonFileCommand;
    [ObservableProperty] private RelayCommand _saveNotUploadedToUploadJsonFileCommand;
    [ObservableProperty] private RelayCommand _saveSelectedToUploadJsonFileCommand;
    [ObservableProperty] private RelayCommand _startAllUploadsCommand;
    [ObservableProperty] private RelayCommand _startFailedUploadsCommand;
    [ObservableProperty] private RelayCommand _startSelectedUploadsCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand _toClipboardAllItemsCommand;
    [ObservableProperty] private RelayCommand _toClipboardSelectedItemsCommand;
    [ObservableProperty] private RelayCommand _toExcelAllItemsCommand;
    [ObservableProperty] private RelayCommand _toExcelSelectedItemsCommand;
    [ObservableProperty] private S3UploadsUploadBatch? _uploadBatch;
    [ObservableProperty] private S3AccountInformation _uploadS3Information;

    private S3UploadsContext(StatusControlContext? statusContext, S3AccountInformation s3Info,
        WindowIconStatus? osStatusIndicator)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _osStatusIndicator = osStatusIndicator;
        _uploadS3Information = s3Info;

        _startSelectedUploadsCommand = StatusContext.RunNonBlockingTaskCommand(StartSelectedUploads);
        _startFailedUploadsCommand = StatusContext.RunNonBlockingTaskCommand(StartFailedUploads);
        _startAllUploadsCommand = StatusContext.RunNonBlockingTaskCommand(StartAllUploads);
        _clearUploadedCommand = StatusContext.RunNonBlockingTaskCommand(ClearUploaded);

        _saveAllToUploadJsonFileCommand = StatusContext.RunNonBlockingTaskCommand(SaveAllToUploadJsonFile);
        _saveSelectedToUploadJsonFileCommand = StatusContext.RunNonBlockingTaskCommand(SaveSelectedToUploadJsonFile);
        _saveNotUploadedToUploadJsonFileCommand =
            StatusContext.RunNonBlockingTaskCommand(SaveNotUploadedToUploadJsonFile);
        _openLocalFileInExplorerCommand =
            StatusContext.RunBlockingTaskCommand<S3UploadsItem>(async x => await OpenLocalFileInExplorer(x));

        _toExcelAllItemsCommand =
            StatusContext.RunNonBlockingTaskCommand(async () => await ItemsToExcel(Items?.ToList()));
        _toExcelSelectedItemsCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            await ItemsToExcel(ListSelection?.SelectedItems?.ToList()));
        _toClipboardAllItemsCommand =
            StatusContext.RunNonBlockingTaskCommand(async () => await ItemsToClipboard(Items?.ToList()));
        _toClipboardSelectedItemsCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            await ItemsToClipboard(ListSelection?.SelectedItems?.ToList()));

        _removeSelectedItemsCommand = StatusContext.RunBlockingTaskCommand(RemoveSelectedItems);
        _clearCompletedUploadBatch = StatusContext.RunNonBlockingActionCommand(() =>
        {
            if (UploadBatch is { Completed: true }) UploadBatch = null;
        });
    }

    public async Task ClearUploaded()
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null) return;

        var toRemove = Items.Where(x => x is { HasError: false, Completed: true }).ToList();

        await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();

        toRemove.ForEach(x => Items.Remove(x));
    }

    public static async Task<S3UploadsContext> CreateInstance(StatusControlContext statusContext,
        S3AccountInformation s3Info, List<S3UploadRequest> uploadList, WindowIconStatus? windowStatus)
    {
        var newControl = new S3UploadsContext(statusContext, s3Info, windowStatus);
        await newControl.LoadData(uploadList);

        return newControl;
    }

    private async Task FileItemsToS3UploaderJsonFile(List<S3UploadsItem> items)
    {
        if (!items.Any()) return;

        //var fileName = Path.Combine(UserSettingsSingleton.CurrentSettings().LocalScriptsDirectory().FullName,
        //    $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Data.json");

        var fileName = UploadS3Information.FullFileNameForJsonUploadInformation();

        var jsonInfo = JsonSerializer.Serialize(items.Select(x =>
            new S3UploadFileEntry(x.FileToUpload.FullName, x.AmazonObjectKey, x.UploadS3Information.BucketName(),
                x.UploadS3Information.BucketRegion(), x.Note)));

        var file = new FileInfo(fileName);

        await File.WriteAllTextAsync(file.FullName, jsonInfo);

        await ProcessHelpers.OpenExplorerWindowForFile(fileName).ConfigureAwait(false);
    }

    public async Task ItemsToClipboard(List<S3UploadsItem>? items)
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (items == null || !items.Any())
        {
            StatusContext.ToastError("No items?");
            return;
        }

        var itemsForClipboard = string.Join(Environment.NewLine,
            items.Select(x =>
                    $"{x.FileToUpload.FullName}\t{x.UploadS3Information.BucketName()}\t{x.AmazonObjectKey}\tCompleted: {x.Completed}\tHas Error: {x.HasError}\t Error: {x.ErrorMessage}")
                .ToList());

        await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(itemsForClipboard);

        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.ToastSuccess("Items added to the Clipboard");
    }

    // ReSharper disable NotAccessedPositionalProperty.Global Properties accessed by reflection
    public record S3ExcelUploadInformation(string FullName, string AmazonObjectKey, string BucketName, bool Completed,
        bool HasError, string ErrorMessage);
    // ReSharper restore NotAccessedPositionalProperty.Global

    public async Task ItemsToExcel(List<S3UploadsItem>? items)
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (items == null || !items.Any())
        {
            StatusContext.ToastError("No items?");
            return;
        }

        var itemsForExcel = items.Select(x => new S3ExcelUploadInformation(x.FileToUpload.FullName, x.AmazonObjectKey,
            x.UploadS3Information.BucketName(), x.Completed, x.HasError, x.ErrorMessage)).ToList();

        ExcelTools.ToExcelFileAsTable(itemsForExcel.Cast<object>().ToList(),
            UploadS3Information.FullFileNameForToExcel());
    }

    public async Task LoadData(List<S3UploadRequest> uploadList)
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (!uploadList.Any()) return;

        ListSelection = await ContentListSelected<S3UploadsItem>.CreateInstance(StatusContext);

        var newItemsList = uploadList
            .Select(x => new S3UploadsItem(UploadS3Information, x.ToUpload.LocalFile, x.S3Key, x.Note))
            .OrderByDescending(x => x.FileToUpload.FullName.Count(y => y == '\\')).ThenBy(x => x.FileToUpload.FullName)
            .ToList();

        await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();

        if (Items == null)
        {
            Items = new ObservableCollection<S3UploadsItem>(newItemsList);
        }
        else
        {
            Items.Clear();
            newItemsList.ForEach(x => Items.Add(x));
        }
    }

    public async Task OpenLocalFileInExplorer(S3UploadsItem? toOpen)
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();

        if (toOpen == null)
        {
            StatusContext.ToastWarning("Nothing selection or Item doesn't exist???");
            return;
        }

        await ProcessHelpers.OpenExplorerWindowForFile(toOpen.FileToUpload.FullName);
    }

    public async Task RemoveSelectedItems()
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || ListSelection == null)
        {
            StatusContext.ToastError("Nothing to delete?");
            return;
        }

        var canDelete = ListSelection?.SelectedItems?.Where(x => x is { Queued: false, IsUploading: false }).ToList() ??
                        new List<S3UploadsItem>();

        if (canDelete.Count == 0)
        {
            StatusContext.ToastError("Everything selected is queued or uploading - can't delete anything...");
            return;
        }

        await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopDeletes in canDelete) Items.Remove(loopDeletes);

        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.ToastSuccess($"{canDelete.Count} Items Removed");
    }

    public async Task SaveAllToUploadJsonFile()
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No Items to Save?");
            return;
        }

        await FileItemsToS3UploaderJsonFile(Items.ToList());
    }

    public async Task SaveNotUploadedToUploadJsonFile()
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null || !Items.Any(x => x is { Completed: true, HasError: false }))
        {
            StatusContext.ToastError("No Items to Save?");
            return;
        }

        await FileItemsToS3UploaderJsonFile(Items.Where(x => x is { Completed: true, HasError: false }).ToList());
    }

    public async Task SaveSelectedToUploadJsonFile()
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (ListSelection?.SelectedItems == null || !ListSelection.SelectedItems.Any())
        {
            StatusContext.ToastError("No Items to Save?");
            return;
        }

        await FileItemsToS3UploaderJsonFile(ListSelection.SelectedItems);
    }

    public async Task StartAllUploads()
    {
        if (UploadBatch is { Completed: false })
        {
            StatusContext.ToastWarning("Wait for the current Upload Batch to Complete...");
            return;
        }

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("Nothing Selected...");
            return;
        }

        var localSelected = Items.ToList();

        UploadBatch = await S3UploadsUploadBatch.CreateInstance(localSelected);
        UploadBatch.PropertyChanged += UploadBatchOnPropertyChanged;

        StatusContext.RunFireAndForgetNonBlockingTask(async () => await UploadBatch.StartUploadBatch());
    }

    public async Task StartFailedUploads()
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeBackgroundAsync();

        if (Items == null) return;

        var toRetry = Items.Where(x => x is { HasError: true }).ToList();

        UploadBatch = await S3UploadsUploadBatch.CreateInstance(toRetry);
        UploadBatch.PropertyChanged += UploadBatchOnPropertyChanged;

        StatusContext.RunFireAndForgetNonBlockingTask(async () => await UploadBatch.StartUploadBatch());
    }


    public async Task StartSelectedUploads()
    {
        if (UploadBatch is { Completed: false })
        {
            StatusContext.ToastWarning("Wait for the current Upload Batch to Complete...");
            return;
        }

        if (ListSelection?.SelectedItems == null || !ListSelection.SelectedItems.Any())
        {
            StatusContext.ToastError("Nothing Selected...");
            return;
        }

        var localSelected = ListSelection.SelectedItems.ToList();

        UploadBatch = await S3UploadsUploadBatch.CreateInstance(localSelected);

        StatusContext.RunFireAndForgetNonBlockingTask(async () => await UploadBatch.StartUploadBatch());
    }

    private void UploadBatchOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UploadBatch) or nameof(S3UploadsUploadBatch.Uploading)
            or nameof(S3UploadsUploadBatch.CompletedSizePercent) or nameof(S3UploadsUploadBatch.CompletedItemPercent))
        {
            if (UploadBatch is not { Uploading: true })
            {
                OsStatusIndicator?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                    TaskbarItemProgressState.None));
                return;
            }

            OsStatusIndicator?.AddRequest(new WindowIconStatusRequest(StatusContext.StatusControlContextId,
                TaskbarItemProgressState.Normal,
                (UploadBatch.CompletedSizePercent + UploadBatch.CompletedItemPercent) / 2));
        }
    }
}