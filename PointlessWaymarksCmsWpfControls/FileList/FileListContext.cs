using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FileList
{
    public class FileListContext : INotifyPropertyChanged
    {
        private ObservableRangeCollection<FileListListItem> _items;
        private List<FileListListItem> _selectedItems;
        private StatusControlContext _statusContext;

        public FileListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public ObservableRangeCollection<FileListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public List<FileListListItem> SelectedItems
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

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var db = await Db.Context();

            var dbItems = db.FileContents.ToList();
            var listItems = new List<FileListListItem>();

            foreach (var loopItems in dbItems)
            {
                var newFileItem = new FileListListItem {DbEntry = loopItems};

                if (loopItems.MainPicture != null)
                    newFileItem.SmallImageUrl = PictureAssetProcessing
                        .ProcessPictureDirectory(loopItems.MainPicture.Value).SmallPicture?.File.FullName;

                listItems.Add(newFileItem);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableRangeCollection<FileListListItem>(listItems);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}