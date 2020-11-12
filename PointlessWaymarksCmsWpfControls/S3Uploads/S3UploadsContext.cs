#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.S3Uploads
{
    public class S3UploadsContext : INotifyPropertyChanged
    {
        private ObservableCollection<S3UploadsItem>? _items;
        private List<S3UploadsItem> _selectedItems = new List<S3UploadsItem>();
        private Command? _startAllUploadsCommand;
        private Command? _startSelectedUploadsCommand;
        private StatusControlContext _statusContext;
        private S3UploadsUploadBatch? _uploadBatch;

        public S3UploadsContext(StatusControlContext? statusContext)
        {
            _statusContext = statusContext ?? new StatusControlContext();

            StartSelectedUploadsCommand = StatusContext.RunBlockingTaskCommand(StartSelectedUploads);
            StartAllUploadsCommand = StatusContext.RunBlockingTaskCommand(StartAllUploads);
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