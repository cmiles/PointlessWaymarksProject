#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Amazon.S3;
using Amazon.S3.Model;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.Status;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.Aws;
using PointlessWaymarks.CmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.S3Deletions
{
    public class S3DeletionsContext : INotifyPropertyChanged
    {
        private Command _deleteAllCommand;
        private Command _deleteSelectedCommand;
        private ObservableCollection<S3DeletionsItem>? _items;
        private List<S3DeletionsItem> _selectedItems = new();
        private StatusControlContext _statusContext;
        private Command _toClipboardAllItemsCommand;
        private Command _toClipboardSelectedItemsCommand;
        private Command _toExcelAllItemsCommand;
        private Command _toExcelSelectedItemsCommand;

        public S3DeletionsContext(StatusControlContext? statusContext)
        {
            _statusContext = statusContext ?? new StatusControlContext();
            _deleteAllCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(async x => await DeleteAll(x), "Cancel Deletions");
            _deleteSelectedCommand =
                StatusContext.RunBlockingTaskWithCancellationCommand(async x => await DeleteSelected(x),
                    "Cancel Deletions");

            _toExcelAllItemsCommand =
                StatusContext.RunNonBlockingTaskCommand(async () => await ItemsToExcel(Items?.ToList()));
            _toExcelSelectedItemsCommand =
                StatusContext.RunNonBlockingTaskCommand(async () => await ItemsToExcel(SelectedItems.ToList()));
            _toClipboardAllItemsCommand =
                StatusContext.RunNonBlockingTaskCommand(async () => await ItemsToClipboard(Items?.ToList()));
            _toClipboardSelectedItemsCommand =
                StatusContext.RunNonBlockingTaskCommand(async () => await ItemsToClipboard(SelectedItems.ToList()));
        }

        public Command DeleteAllCommand
        {
            get => _deleteAllCommand;
            set
            {
                if (Equals(value, _deleteAllCommand)) return;
                _deleteAllCommand = value;
                OnPropertyChanged();
            }
        }

        public Command DeleteSelectedCommand
        {
            get => _deleteSelectedCommand;
            set
            {
                if (Equals(value, _deleteSelectedCommand)) return;
                _deleteSelectedCommand = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<S3DeletionsItem>? Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public List<S3DeletionsItem> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (Equals(value, _selectedItems)) return;
                _selectedItems = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public Command ToClipboardAllItemsCommand
        {
            get => _toClipboardAllItemsCommand;
            set
            {
                if (Equals(value, _toClipboardAllItemsCommand)) return;
                _toClipboardAllItemsCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ToClipboardSelectedItemsCommand
        {
            get => _toClipboardSelectedItemsCommand;
            set
            {
                if (Equals(value, _toClipboardSelectedItemsCommand)) return;
                _toClipboardSelectedItemsCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ToExcelAllItemsCommand
        {
            get => _toExcelAllItemsCommand;
            set
            {
                if (Equals(value, _toExcelAllItemsCommand)) return;
                _toExcelAllItemsCommand = value;
                OnPropertyChanged();
            }
        }

        public Command ToExcelSelectedItemsCommand
        {
            get => _toExcelSelectedItemsCommand;
            set
            {
                if (Equals(value, _toExcelSelectedItemsCommand)) return;
                _toExcelSelectedItemsCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static async Task<S3DeletionsContext> CreateInstance(StatusControlContext? statusContext,
            List<S3DeletionsItem> itemsToDelete)
        {
            var newControl = new S3DeletionsContext(statusContext);
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

            var (accessKey, secret) = AwsCredentials.GetAwsSiteCredentials();

            if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secret))
            {
                StatusContext.ToastError("Aws Credentials are not entered or valid?");
                return;
            }

            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            progress.Report("Getting Amazon Client");

            var s3Client = new AmazonS3Client(accessKey, secret,
                UserSettingsSingleton.CurrentSettings().SiteS3BucketEndpoint());

            var loopCount = 0;
            var totalCount = itemsToDelete.Count;

            //Sorted items as a quick way to delete the deepest items first
            var sortedItems = itemsToDelete.OrderByDescending(x => x.AmazonObjectKey.Count(y => y == '/'))
                .ThenByDescending(x => x.AmazonObjectKey.Length).ThenByDescending(x => x.AmazonObjectKey).ToList();

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

        public async Task DeleteAll(CancellationToken cancellationToken)
        {
            await Delete(Items?.ToList() ?? new List<S3DeletionsItem>(), cancellationToken,
                StatusContext.ProgressTracker());
        }

        public async Task DeleteSelected(CancellationToken cancellationToken)
        {
            await Delete(SelectedItems.ToList(), cancellationToken, StatusContext.ProgressTracker());
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
                items.Select(x =>
                        $"{x.BucketName}\t{x.AmazonObjectKey}\tHas Error: {x.HasError}\t Error: {x.ErrorMessage}")
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

            var itemsForExcel = items.Select(x => new {x.BucketName, x.AmazonObjectKey, x.HasError, x.ErrorMessage})
                .ToList();

            ExcelHelpers.ContentToExcelFileAsTable(itemsForExcel.Cast<object>().ToList(), "UploadItemsList");
        }

        public async Task LoadData(List<S3DeletionsItem> toDelete)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableCollection<S3DeletionsItem>(toDelete);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}