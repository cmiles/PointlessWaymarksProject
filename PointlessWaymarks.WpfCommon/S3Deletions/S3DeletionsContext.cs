using System.Collections.ObjectModel;
using System.Windows;
using Amazon.S3;
using Amazon.S3.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.S3Deletions;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class S3DeletionsContext
{
    private S3DeletionsContext(StatusControlContext? statusContext, IS3AccountInformation s3Info)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        BuildCommands();

        UploadS3Information = s3Info;
    }

    public ObservableCollection<S3DeletionsItem>? Items { get; set; }
    public List<S3DeletionsItem> SelectedItems { get; set; } = [];
    public StatusControlContext StatusContext { get; set; }
    public IS3AccountInformation UploadS3Information { get; set; }

    public static async Task<S3DeletionsContext> CreateInstance(StatusControlContext? statusContext,
        IS3AccountInformation s3Info, List<S3DeletionsItem> itemsToDelete)
    {
        var newControl = new S3DeletionsContext(statusContext, s3Info);
        await newControl.LoadData(itemsToDelete);
        return newControl;
    }

    public async Task Delete(List<S3DeletionsItem> itemsToDelete, CancellationToken cancellationToken,
        IProgress<string> progress)
    {
        if (!itemsToDelete.Any())
        {
            StatusContext.ToastError("Nothing to Delete?");
            return;
        }

        progress.Report("Getting Amazon Credentials");
        
        var bucket = UploadS3Information.BucketName();
        var serviceUrl = UploadS3Information.ServiceUrl();
        
        if (string.IsNullOrWhiteSpace(bucket))
        {
            StatusContext.ToastError("S3 Bucket is Blank?");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            StatusContext.ToastError("S3 Service URL is empty?");
            return;
        }

        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

        progress.Report("Getting Amazon Client");

        var s3Client = UploadS3Information.S3Client();

        var loopCount = 0;
        var totalCount = itemsToDelete.Count;

        //Sorted items as a quick way to delete the deepest items first
        var sortedItems = itemsToDelete.OrderByDescending(x => x.AmazonObjectKey.Count(y => y == '/'))
            .ThenByDescending(x => x.AmazonObjectKey.Length)
            .ThenByDescending<S3DeletionsItem, string>(x => x.AmazonObjectKey).ToList();

        foreach (var loopDeletionItems in sortedItems)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            if (++loopCount % 10 == 0)
                progress.Report($"S3 Deletion {loopCount} of {totalCount} - {loopDeletionItems.AmazonObjectKey}");

            try
            {
                await s3Client.DeleteObjectAsync(
                    new DeleteObjectRequest
                    {
                        BucketName = loopDeletionItems.BucketName, Key = loopDeletionItems.AmazonObjectKey
                    }, cancellationToken);
            }
            catch (Exception e)
            {
                progress.Report($"S3 Deletion Error - {loopDeletionItems.AmazonObjectKey} - {e.Message}");
                loopDeletionItems.HasError = true;
                loopDeletionItems.ErrorMessage = e.Message;
            }
        }

        var toRemoveFromList = itemsToDelete.Where(x => !x.HasError).ToList();

        progress.Report($"Removing {toRemoveFromList.Count} successfully deleted items from the list...");

        if (Items == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        toRemoveFromList.ForEach(x => Items.Remove(x));
    }

    [BlockingCommand]
    public async Task DeleteAll(CancellationToken cancellationToken)
    {
        await Delete( Items?.ToList() ?? [], cancellationToken,
            StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task DeleteSelected(CancellationToken cancellationToken)
    {
        await Delete( SelectedItems.ToList(), cancellationToken,
            StatusContext.ProgressTracker());
    }

    public async Task ItemsToClipboard(List<S3DeletionsItem>? items)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (items == null || !items.Any())
        {
            StatusContext.ToastError("No items?");
            return;
        }

        var itemsForClipboard = string.Join(Environment.NewLine,
            items.Select(x => $"{x.BucketName}\t{x.AmazonObjectKey}\tHas Error: {x.HasError}\t Error: {x.ErrorMessage}")
                .ToList());

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(itemsForClipboard);
    }

    public async Task ItemsToExcel(List<S3DeletionsItem>? items)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (items == null || !items.Any())
        {
            StatusContext.ToastError("No items?");
            return;
        }

        var itemsForExcel = items.Select(x => new { x.BucketName, x.AmazonObjectKey, x.HasError, x.ErrorMessage })
            .ToList();

        ExcelTools.ToExcelFileAsTable(itemsForExcel.Cast<object>().ToList(),
            UploadS3Information.FullFileNameForToExcel());
    }

    public async Task LoadData(List<S3DeletionsItem> toDelete)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items = new ObservableCollection<S3DeletionsItem>(toDelete);
    }

    [NonBlockingCommand]
    public async Task ToClipboardAllItems()
    {
        await ItemsToClipboard(Items?.ToList());
    }

    [NonBlockingCommand]
    public async Task ToClipboardSelectedItems()
    {
        await ItemsToClipboard(SelectedItems.ToList());
    }

    [NonBlockingCommand]
    public async Task ToExcelAllItems()
    {
        await ItemsToExcel(Items?.ToList());
    }

    [NonBlockingCommand]
    public async Task ToExcelSelectedItems()
    {
        await ItemsToExcel(SelectedItems.ToList());
    }
}