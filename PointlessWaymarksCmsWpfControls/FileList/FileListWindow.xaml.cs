using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.FileHtml;
using PointlessWaymarksCmsWpfControls.FileContentEditor;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FileList
{
    public partial class FileListWindow : INotifyPropertyChanged
    {
        private RelayCommand _editSelectedContentCommand;
        private RelayCommand _generateSelectedHtmlCommand;
        private FileListContext _listContext;
        private RelayCommand _openUrlForSelectedCommand;
        private RelayCommand _photoCodesToClipboardForSelectedCommand;
        private StatusControlContext _statusContext;

        public FileListWindow()
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            ListContext = new FileListContext(StatusContext);

            GenerateSelectedHtmlCommand = new RelayCommand(() => StatusContext.RunBlockingTask(GenerateSelectedHtml));
            EditSelectedContentCommand = new RelayCommand(() => StatusContext.RunBlockingTask(EditSelectedContent));
            FileCodesToClipboardForSelectedCommand =
                new RelayCommand(() => StatusContext.RunBlockingTask(FileCodesToClipboardForSelected));
            OpenUrlForSelectedCommand = new RelayCommand(() => StatusContext.RunNonBlockingTask(OpenUrlForSelected));

            DataContext = this;
        }

        public RelayCommand EditSelectedContentCommand
        {
            get => _editSelectedContentCommand;
            set
            {
                if (Equals(value, _editSelectedContentCommand)) return;
                _editSelectedContentCommand = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand FileCodesToClipboardForSelectedCommand
        {
            get => _photoCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _photoCodesToClipboardForSelectedCommand)) return;
                _photoCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
        }


        public RelayCommand GenerateSelectedHtmlCommand
        {
            get => _generateSelectedHtmlCommand;
            set
            {
                if (Equals(value, _generateSelectedHtmlCommand)) return;
                _generateSelectedHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public FileListContext ListContext
        {
            get => _listContext;
            set
            {
                if (Equals(value, _listContext)) return;
                _listContext = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand OpenUrlForSelectedCommand
        {
            get => _openUrlForSelectedCommand;
            set
            {
                if (Equals(value, _openUrlForSelectedCommand)) return;
                _openUrlForSelectedCommand = value;
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

        private async Task EditSelectedContent()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                var newContentWindow =
                    new FileContentEditorWindow(loopSelected.DbEntry) {Left = Left + 4, Top = Top + 4};

                newContentWindow.Show();

                await ThreadSwitcher.ResumeBackgroundAsync();
            }
        }

        private async Task FileCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in ListContext.SelectedItems)
                finalString +=
                    @$"{{{{filelink {loopSelected.DbEntry.ContentId}; {loopSelected.DbEntry.Title}}}}}{Environment.NewLine}";

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);

            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }

        private async Task GenerateSelectedHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var loopCount = 1;
            var totalCount = ListContext.SelectedItems.Count;

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                StatusContext.Progress(
                    $"Generating Html for {loopSelected.DbEntry.Title}, {loopCount} of {totalCount}");

                var htmlContext = new SingleFilePage(loopSelected.DbEntry);

                htmlContext.WriteLocalHtml();

                StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");

                loopCount++;
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OpenUrlForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var settings = await UserSettingsUtilities.ReadSettings();

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                var url = $@"http://{settings.FilePageUrl(loopSelected.DbEntry)}";

                var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
                Process.Start(ps);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}