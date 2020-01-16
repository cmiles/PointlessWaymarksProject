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

namespace PointlessWaymarksCmsWpfControls.ImageList
{
    public class ImageListContext : INotifyPropertyChanged
    {
        private ObservableRangeCollection<ImageListListItem> _items;
        private List<ImageListListItem> _selectedItems;
        private StatusControlContext _statusContext;

        public ImageListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public ObservableRangeCollection<ImageListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public List<ImageListListItem> SelectedItems
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

            var dbItems = db.ImageContents.ToList();
            var listItems = new List<ImageListListItem>();

            foreach (var loopItems in dbItems)
            {
                var newImageItem = new ImageListListItem
                {
                    DbEntry = loopItems,
                    SmallImageUrl = PictureAssetProcessing.ProcessImageDirectory(loopItems).SmallPicture?.File
                        .FullName
                };

                listItems.Add(newImageItem);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableRangeCollection<ImageListListItem>(listItems);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}