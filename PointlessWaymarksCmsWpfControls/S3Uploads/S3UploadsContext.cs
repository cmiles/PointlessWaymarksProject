#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.S3Uploads
{
    public class S3UploadsContext : INotifyPropertyChanged
    {
        private ObservableCollection<S3UploadsItem>? _items;
        private Command? _saveAllToUploadJsonFileCommand;
        private Command? _saveNotUploadedToUploadJsonFileCommand;
        private Command? _saveSelectedToUploadJsonFileCommand;
        private List<S3UploadsItem> _selectedItems = new();
        private Command? _startAllUploadsCommand;
        private Command? _startSelectedUploadsCommand;
        private StatusControlContext _statusContext;
        private S3UploadsUploadBatch? _uploadBatch;

        public S3UploadsContext(StatusControlContext? statusContext)
        {
            _statusContext = statusContext ?? new StatusControlContext();

            StartSelectedUploadsCommand = StatusContext.RunNonBlockingTaskCommand(StartSelectedUploads);
            StartAllUploadsCommand = StatusContext.RunNonBlockingTaskCommand(StartAllUploads);

            SaveAllToUploadJsonFileCommand = StatusContext.RunNonBlockingTaskCommand(SaveAllToUploadJsonFile);
            SaveSelectedToUploadJsonFileCommand = StatusContext.RunNonBlockingTaskCommand(SaveSelectedToUploadJsonFile);
            SaveNotUploadedToUploadJsonFileCommand =
                StatusContext.RunNonBlockingTaskCommand(SaveNotUploadedToUploadJsonFile);
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

        public Command? SaveAllToUploadJsonFileCommand
        {
            get => _saveAllToUploadJsonFileCommand;
            set
            {
                if (Equals(value, _saveAllToUploadJsonFileCommand)) return;
                _saveAllToUploadJsonFileCommand = value;
                OnPropertyChanged();
            }
        }

        public Command? SaveNotUploadedToUploadJsonFileCommand
        {
            get => _saveNotUploadedToUploadJsonFileCommand;
            set
            {
                if (Equals(value, _saveNotUploadedToUploadJsonFileCommand)) return;
                _saveNotUploadedToUploadJsonFileCommand = value;
                OnPropertyChanged();
            }
        }

        public Command? SaveSelectedToUploadJsonFileCommand
        {
            get => _saveSelectedToUploadJsonFileCommand;
            set
            {
                if (Equals(value, _saveSelectedToUploadJsonFileCommand)) return;
                _saveSelectedToUploadJsonFileCommand = value;
                OnPropertyChanged();
            }
        }

        public List<S3UploadsItem> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (Equals(value, _selectedItems)) return;
                _selectedItems = value;
                OnPropertyChanged();
            }
        }

        public Command? StartAllUploadsCommand
        {
            get => _startAllUploadsCommand;
            set
            {
                if (Equals(value, _startAllUploadsCommand)) return;
                _startAllUploadsCommand = value;
                OnPropertyChanged();
            }
        }

        public Command? StartSelectedUploadsCommand
        {
            get => _startSelectedUploadsCommand;
            set
            {
                if (Equals(value, _startSelectedUploadsCommand)) return;
                _startSelectedUploadsCommand = value;
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

        public S3UploadsUploadBatch? UploadBatch
        {
            get => _uploadBatch;
            set
            {
                if (Equals(value, _uploadBatch)) return;
                _uploadBatch = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static async Task<S3UploadsContext> CreateInstance(StatusControlContext statusContext,
            List<S3Upload> uploadList)
        {
            var newControl = new S3UploadsContext(statusContext);
            await newControl.LoadData(uploadList);
            return newControl;
        }

        private async Task FileItemsToS3UploaderJsonFile(List<S3UploadsItem> items)
        {
            if (!items.Any()) return;

            var fileName = Path.Combine(UserSettingsSingleton.CurrentSettings().LocalSiteScriptsDirectory().FullName,
                $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---File-Upload-Data.json");

            var jsonInfo = JsonSerializer.Serialize(items.Select(x =>
                new S3UploadFileRecord(x.FileToUpload.FullName, x.AmazonObjectKey, x.BucketName, x.Note)));

            var file = new FileInfo(fileName);

            await File.WriteAllTextAsync(file.FullName, jsonInfo);

            await ProcessHelpers.OpenExplorerWindowForFile(fileName).ConfigureAwait(false);
        }

        public async Task LoadData(List<S3Upload> uploadList)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!uploadList.Any()) return;

            var newItemsList = uploadList.Select(x => new S3UploadsItem(x.ToUpload, x.S3Key, x.BucketName, x.Note))
                .OrderByDescending(x => x.FileToUpload.FullName.Count(y => y == '\\'))
                .ThenBy(x => x.FileToUpload.FullName).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();

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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task SaveAllToUploadJsonFile()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any())
            {
                StatusContext.ToastError("No Items to Save?");
                return;
            }

            await FileItemsToS3UploaderJsonFile(Items.ToList());
        }

        public async Task SaveNotUploadedToUploadJsonFile()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (Items == null || !Items.Any(x => x.Completed && !x.HasError))
            {
                StatusContext.ToastError("No Items to Save?");
                return;
            }

            await FileItemsToS3UploaderJsonFile(Items.Where(x => x.Completed && !x.HasError).ToList());
        }

        public async Task SaveSelectedToUploadJsonFile()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (!SelectedItems.Any())
            {
                StatusContext.ToastError("No Items to Save?");
                return;
            }

            await FileItemsToS3UploaderJsonFile(SelectedItems);
        }

        public async Task StartAllUploads()
        {
            if (UploadBatch != null && !UploadBatch.Completed)
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

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await UploadBatch.StartUploadBatch());
        }

        public async Task StartSelectedUploads()
        {
            if (UploadBatch != null && !UploadBatch.Completed)
            {
                StatusContext.ToastWarning("Wait for the current Upload Batch to Complete...");
                return;
            }

            if (!SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected...");
                return;
            }

            var localSelected = SelectedItems.ToList();

            UploadBatch = await S3UploadsUploadBatch.CreateInstance(localSelected);

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await UploadBatch.StartUploadBatch());
        }
    }
}