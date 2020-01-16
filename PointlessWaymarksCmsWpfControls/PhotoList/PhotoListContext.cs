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

namespace PointlessWaymarksCmsWpfControls.PhotoList
{
    public class PhotoListContext : INotifyPropertyChanged
    {
        private ObservableRangeCollection<PhotoListListItem> _items;
        private List<PhotoListListItem> _selectedItems;
        private StatusControlContext _statusContext;

        public PhotoListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public ObservableRangeCollection<PhotoListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public List<PhotoListListItem> SelectedItems
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

            var dbItems = db.PhotoContents.ToList();
            var listItems = new List<PhotoListListItem>();

            foreach (var loopItems in dbItems)
            {
                var newPhotoItem = new PhotoListListItem
                {
                    DbEntry = loopItems,
                    SmallImageUrl = PictureAssetProcessing.ProcessPhotoDirectory(loopItems).SmallPicture?.File
                        .FullName
                };

                listItems.Add(newPhotoItem);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableRangeCollection<PhotoListListItem>(listItems);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}