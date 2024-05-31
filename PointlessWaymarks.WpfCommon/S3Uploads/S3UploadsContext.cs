using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Shell;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.S3Uploads;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class S3UploadsContext
{
    private S3UploadsContext(StatusControlContext? statusContext, IS3AccountInformation s3Info,
        ObservableCollection<S3UploadsItem> items, ContentListSelected<S3UploadsItem> listSelection,
        WindowIconStatus? osStatusIndicator)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        OsStatusIndicator = osStatusIndicator;
        UploadS3Information = s3Info;
        Items = items;
        ListSelection = listSelection;

        BuildCommands();
    }

    public ObservableCollection<S3UploadsItem> Items { get; set; }
    public ContentListSelected<S3UploadsItem>? ListSelection { get; set; }
    public WindowIconStatus? OsStatusIndicator { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public S3UploadsUploadBatch? UploadBatch { get; set; }
    public IS3AccountInformation UploadS3Information { get; set; }

    public List<S3UploadsItem> SelectedListItems()
    {
        return ListSelection?.SelectedItems ?? [];
    }

    [NonBlockingCommand]
    public async Task ClearCompletedUploadBatch()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (UploadBatch is { Completed: true }) UploadBatch = null;
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoItems]
    public async Task ClearUploaded()
    {
        var toRemove = Items.Where(x => x is { HasError: false, Completed: true }).ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        toRemove.ForEach(x => Items.Remove(x));
    }

    public static async Task<S3UploadsContext> CreateInstance(StatusControlContext statusContext,
        IS3AccountInformation s3Info, List<S3UploadRequest> uploadList, WindowIconStatus? windowStatus)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryItems = new ObservableCollection<S3UploadsItem>();
        var factorySelection = await ContentListSelected<S3UploadsItem>.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var newControl = new S3UploadsContext(statusContext, s3Info, factoryItems, factorySelection, windowStatus);
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
                x.UploadS3Information.ServiceUrl(), x.Note)));

        var file = new FileInfo(fileName);

        await File.WriteAllTextAsync(file.FullName, jsonInfo);

        await ProcessHelpers.OpenExplorerWindowForFile(fileName).ConfigureAwait(false);
    }

    public async Task ItemsToClipboard(List<S3UploadsItem>? items)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (items == null || !items.Any())
        {
            StatusContext.ToastError("No items?");
            return;
        }

        var itemsForClipboard = string.Join(Environment.NewLine,
            items.Select(x =>
                    $"{x.FileToUpload.FullName}\t{x.UploadS3Information.BucketName()}\t{x.AmazonObjectKey}\tCompleted: {x.Completed}\tHas Error: {x.HasError}\t Error: {x.ErrorMessage}")
                .ToList());

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(itemsForClipboard);

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.ToastSuccess("Items added to the Clipboard");
    }

    [NonBlockingCommand]
    public async Task ToClipboardAllItems()
    {
        await ItemsToClipboard(Items?.ToList());
    }

    [NonBlockingCommand]
    public async Task ToClipboardSelectedItems()
    {
        await ItemsToClipboard(SelectedListItems());
    }

    [NonBlockingCommand]
    public async Task ToExcelAllItems()
    {
        await ItemsToExcel(Items?.ToList());
    }

    [NonBlockingCommand]
    public async Task ToExcelSelectedItems()
    {
        await ItemsToExcel(SelectedListItems());
    }

    // ReSharper disable NotAccessedPositionalProperty.Global Properties accessed by reflection
    public record S3ExcelUploadInformation(
        string FullName,
        string AmazonObjectKey,
        string BucketName,
        bool Completed,
        bool HasError,
        string ErrorMessage);
    // ReSharper restore NotAccessedPositionalProperty.Global

    public async Task ItemsToExcel(List<S3UploadsItem>? items)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

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
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newItemsList = uploadList
            .Select(x => new S3UploadsItem(UploadS3Information, x.ToUpload.LocalFile, x.S3Key, x.Note))
            .OrderByDescending(x => x.FileToUpload.FullName.Count(y => y == '\\')).ThenBy(x => x.FileToUpload.FullName)
            .ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();
        newItemsList.ForEach(x => Items.Add(x));
    }

    [BlockingCommand]
    public async Task OpenLocalFileInExplorer(S3UploadsItem? toOpen)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (toOpen == null)
        {
            StatusContext.ToastWarning("Nothing selection or Item doesn't exist???");
            return;
        }

        await ProcessHelpers.OpenExplorerWindowForFile(toOpen.FileToUpload.FullName);
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task RemoveSelectedItems()
    {
        var canDelete = SelectedListItems().Where(x => x is { Queued: false, IsUploading: false }).ToList();

        if (canDelete.Count == 0)
        {
            StatusContext.ToastError("Everything selected is queued or uploading - can't delete anything...");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopDeletes in canDelete) Items.Remove(loopDeletes);

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.ToastSuccess($"{canDelete.Count} Items Removed");
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoItems]
    public async Task SaveAllToUploadJsonFile()
    {
        await FileItemsToS3UploaderJsonFile(Items.ToList());
    }

    [NonBlockingCommand]
    public async Task SaveNotUploadedToUploadJsonFile()
    {
        if (!Items.Any(x => x is { Completed: true, HasError: false }))
        {
            StatusContext.ToastError("No Items to Save?");
            return;
        }

        await FileItemsToS3UploaderJsonFile(Items.Where(x => x is { Completed: true, HasError: false }).ToList());
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task SaveSelectedToUploadJsonFile()
    {
        await FileItemsToS3UploaderJsonFile(SelectedListItems());
    }

    [BlockingCommand]
    public async Task StartAllUploads()
    {
        if (UploadBatch is { Completed: false })
        {
            StatusContext.ToastWarning("Wait for the current Upload Batch to Complete...");
            return;
        }

        if (!Items.Any())
        {
            StatusContext.ToastError("Nothing Selected...");
            return;
        }

        var localSelected = Items.ToList();

        UploadBatch = await S3UploadsUploadBatch.CreateInstance(localSelected);
        UploadBatch.PropertyChanged += UploadBatchOnPropertyChanged;

        StatusContext.RunFireAndForgetNonBlockingTask(async () => await UploadBatch.StartUploadBatch());
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoItems]
    public async Task StartFailedUploads()
    {
        var toRetry = Items.Where(x => x is { HasError: true }).ToList();

        UploadBatch = await S3UploadsUploadBatch.CreateInstance(toRetry);
        UploadBatch.PropertyChanged += UploadBatchOnPropertyChanged;

        StatusContext.RunFireAndForgetNonBlockingTask(async () => await UploadBatch.StartUploadBatch());
    }

    [NonBlockingCommand]
    public async Task StartSelectedUploads()
    {
        if (UploadBatch is { Completed: false })
        {
            StatusContext.ToastWarning("Wait for the current Upload Batch to Complete...");
            return;
        }

        if (!SelectedListItems().Any())
        {
            StatusContext.ToastError("Nothing Selected...");
            return;
        }

        var localSelected = SelectedListItems();

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