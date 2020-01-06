using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers;
using TheLemmonWorkshopData;
using TheLemmonWorkshopData.PhotoHtml;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.PhotoList
{
    public class PhotoListContext : INotifyPropertyChanged
    {
        private ObservableRangeCollection<PhotoListListItem> _items;
        private StatusControlContext _statusContext;
        private List<PhotoListListItem> _selectedItems;
        public event PropertyChangedEventHandler PropertyChanged;

        public PhotoListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
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
                    SmallImageUrl = PhotoFiles.ProcessPhotosInDirectory(loopItems).SmallImage?.File.FullName
                };

                listItems.Add(newPhotoItem);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableRangeCollection<PhotoListListItem>(listItems);
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}