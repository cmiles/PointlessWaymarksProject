using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Ookii.Dialogs.Wpf;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.ImageResizeAndUpload
{
    public class ImageResizeAndUploadContext : INotifyPropertyChanged
    {
        private ObservableCollection<ImageResizeAndUploadListItem> _items;
        private ControlStatusViewModel _statusContext;

        public ImageResizeAndUploadContext()
        {
            StatusContext = new ControlStatusViewModel();
            ChooseAndLoadDirectoryCommand = new Command(() => StatusContext.RunBlockingTask(ChooseAndLoadDirectory));
        }

        public Command ChooseAndLoadDirectoryCommand { get; set; }

        public ObservableCollection<ImageResizeAndUploadListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public ControlStatusViewModel StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task ChooseAndLoadDirectory()
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var directoryPicker = new VistaFolderBrowserDialog();

            if (!(directoryPicker.ShowDialog() ?? false)) return;

            var directory = new DirectoryInfo(directoryPicker.SelectedPath);

            if (!directory.Exists)
            {
                StatusContext.ToastWarning("Directory Doesn't Exist?");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            var filesToAdd = directory.EnumerateFiles().Where(x => x.Extension.ToLower() == ".jpg").OrderBy(x => x.Name)
                .Select(x => new ImageResizeAndUploadListItem {FileItem = x}).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Clear();
            filesToAdd.ForEach(x => Items.Add(x));
        }

        public async Task ProcessSelected()
        {
            if (Items == null || !Items.Any(x => x.Selected))
            {
                StatusContext.ToastWarning("Nothing Selected?");
                return;
            }

            await ThreadSwitcher.ResumeBackgroundAsync();

            var frozenSelected = Items.Where(x => x.Selected).ToList();

            foreach (var loopFile in frozenSelected)
            {
                //ImageResizing.ResizeImageForSrcsetAndUpload()
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}