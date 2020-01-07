using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using GalaSoft.MvvmLight.Command;
using JetBrains.Annotations;
using TheLemmonWorkshopData.PhotoHtml;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.PhotoContentEditor;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.PhotoList
{
    public partial class PhotoListWindow : INotifyPropertyChanged
    {
        private PhotoListContext _listContext;
        private StatusControlContext _statusContext;
        private RelayCommand _photoCodesToClipboardForSelectedCommand;
        private RelayCommand _editSelectedContentCommand;
        private RelayCommand _generateSelectedHtmlCommand;

        public PhotoListWindow()
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            ListContext = new PhotoListContext(StatusContext);

            GenerateSelectedHtmlCommand = new RelayCommand(() => StatusContext.RunBlockingTask(GenerateSelectedHtml));
            EditSelectedContentCommand = new RelayCommand(() => StatusContext.RunBlockingTask(EditSelectedContent));
            PhotoCodesToClipboardForSelectedCommand =
                new RelayCommand(() => StatusContext.RunBlockingTask(PhotoCodesToClipboardForSelected));

            DataContext = this;
        }

        private async Task PhotoCodesToClipboardForSelected()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            var finalString = string.Empty;

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                finalString +=
                    @$"{{{{photo {loopSelected.DbEntry.ContentId}; {loopSelected.DbEntry.Title}}}}}{Environment.NewLine}";
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(finalString);
        }

        public RelayCommand PhotoCodesToClipboardForSelectedCommand
        {
            get => _photoCodesToClipboardForSelectedCommand;
            set
            {
                if (Equals(value, _photoCodesToClipboardForSelectedCommand)) return;
                _photoCodesToClipboardForSelectedCommand = value;
                OnPropertyChanged();
            }
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

        private async Task GenerateSelectedHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (ListContext.SelectedItems == null || !ListContext.SelectedItems.Any())
            {
                StatusContext.ToastError("Nothing Selected?");
                return;
            }

            foreach (var loopSelected in ListContext.SelectedItems)
            {
                var htmlContext = new SinglePhotoPage(loopSelected.DbEntry);

                htmlContext.WriteLocalHtml();
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
                    new PhotoContentEditorWindow(loopSelected.DbEntry) {Left = Left + 4, Top = Top + 4};

                newContentWindow.Show();

                await ThreadSwitcher.ResumeBackgroundAsync();
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

        public PhotoListContext ListContext
        {
            get => _listContext;
            set
            {
                if (Equals(value, _listContext)) return;
                _listContext = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}