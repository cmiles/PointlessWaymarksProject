#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.S3Deletions
{
    public class S3DeletionsContext : INotifyPropertyChanged
    {
        private Command _deleteAllCommand;
        private Command _deleteSelectedCommand;
        private ObservableCollection<S3DeletionsItem>? _items;
        private List<S3DeletionsItem> _selectedItems = new();
        private StatusControlContext _statusContext;

        public S3DeletionsContext(StatusControlContext? statusContext)
        {
            _statusContext = statusContext ?? new StatusControlContext();
            _deleteAllCommand = StatusContext.RunBlockingTaskCommand(DeleteAll);
            _deleteSelectedCommand = StatusContext.RunBlockingTaskCommand(DeleteSelected);
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public static async Task<S3DeletionsContext> CreateInstance(StatusControlContext? statusContext,
            List<S3DeletionsItem> itemsToDelete)
        {
            var newControl = new S3DeletionsContext(statusContext);
            await newControl.LoadData(itemsToDelete);
            return newControl;
        }

        public async Task Delete(List<S3DeletionsItem> itemsToDelete, IProgress<string> progress)
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

            progress.Report("Getting Amazon Client");

            var s3Client = new AmazonS3Client(accessKey, secret);

            var loopCount = 0;
            var totalCount = itemsToDelete.Count;

            //Sorted items as a quick way to delete the deepest items first
            var sortedItems = itemsToDelete.OrderByDescending(x => x.AmazonObjectKey.Count(y => y == '/'))
                .ThenByDescending(x => x.AmazonObjectKey.Length).ThenByDescending(x => x.AmazonObjectKey).ToList();

            foreach (var loopDeletionItems in sortedItems)
            {
                if (++loopCount % 10 == 0)
                    progress.Report($"S3 Deletion {loopCount} of {totalCount} - {loopDeletionItems.AmazonObjectKey}");

                try
                {
                    var deleteResult = await s3Client.DeleteObjectAsync(new DeleteObjectRequest
                    {
                        BucketName = loopDeletionItems.BucketName, Key = loopDeletionItems.AmazonObjectKey
                    });
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

        public async Task DeleteAll()
        {
            await Delete(Items?.ToList() ?? new List<S3DeletionsItem>(), StatusContext.ProgressTracker());
        }

        public async Task DeleteSelected()
        {
            await Delete(SelectedItems.ToList(), StatusContext.ProgressTracker());
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