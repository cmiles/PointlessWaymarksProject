using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.PhotoList
{
    public class PhotoListContext : INotifyPropertyChanged
    {
        private bool _allLoaded;
        private ObservableCollection<PhotoListListItem> _items;
        private string _lastSortColumn;
        private bool _loadRecentOnly;
        private List<PhotoListListItem> _selectedItems;
        private bool _sortDescending;
        private Command<string> _sortListCommand;
        private StatusControlContext _statusContext;
        private Command _toggleListSortDirectionCommand;
        private string _userFilterText;

        public PhotoListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            SortListCommand = new Command<string>(x => StatusContext.RunNonBlockingTask(() => SortList(x)));
            ToggleListSortDirectionCommand = new Command(() => StatusContext.RunNonBlockingTask(async () =>
            {
                SortDescending = !SortDescending;
                await SortList(_lastSortColumn);
            }));
            LoadRecentOnlyCommand = new Command<bool>(x =>
            {
                var currentState = LoadRecentOnly;
                LoadRecentOnly = x;
                if (currentState != LoadRecentOnly) StatusContext.RunBlockingTask(LoadData);
            });

            LoadRecentOnly = true;

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);

            DataNotifications.PhotoContentDataNotificationEvent += DataNotificationsOnContentDataNotificationEvent;
        }

        public bool AllLoaded
        {
            get => _allLoaded;
            set
            {
                if (value == _allLoaded) return;
                _allLoaded = value;
                OnPropertyChanged();
            }
        }


        public ObservableCollection<PhotoListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public bool LoadRecentOnly
        {
            get => _loadRecentOnly;
            set
            {
                if (value == _loadRecentOnly) return;
                _loadRecentOnly = value;
                OnPropertyChanged();
            }
        }

        public Command<bool> LoadRecentOnlyCommand { get; set; }

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

        public bool SortDescending
        {
            get => _sortDescending;
            set
            {
                if (value == _sortDescending) return;
                _sortDescending = value;
                OnPropertyChanged();
            }
        }

        public Command<string> SortListCommand
        {
            get => _sortListCommand;
            set
            {
                if (Equals(value, _sortListCommand)) return;
                _sortListCommand = value;
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

        public Command ToggleListSortDirectionCommand
        {
            get => _toggleListSortDirectionCommand;
            set
            {
                if (Equals(value, _toggleListSortDirectionCommand)) return;
                _toggleListSortDirectionCommand = value;
                OnPropertyChanged();
            }
        }

        public string UserFilterText
        {
            get => _userFilterText;
            set
            {
                if (value == _userFilterText) return;
                _userFilterText = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DataNotificationsOnContentDataNotificationEvent(object sender, DataNotificationEventArgs e)
        {
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () =>
                await DataNotificationsOnContentDataNotificationEvent(e));
        }

        private async Task DataNotificationsOnContentDataNotificationEvent(DataNotificationEventArgs e)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (e.UpdateType == DataNotificationUpdateType.Delete)
            {
                var toRemove = Items.Where(x => e.ContentIds.Contains(x.DbEntry.ContentId)).ToList();

                await ThreadSwitcher.ResumeForegroundAsync();
            }

            if (e.UpdateType == DataNotificationUpdateType.New)
            {
                var context = await Db.Context();

                var toAdd = (await context.PhotoContents.Where(x => e.ContentIds.Contains(x.ContentId)).ToListAsync())
                    .Select(ListItemFromDbItem).ToList();

                await ThreadSwitcher.ResumeForegroundAsync();

                toAdd.ForEach(x => Items.Add(x));
            }

            if (e.UpdateType == DataNotificationUpdateType.Update)
            {
                var context = await Db.Context();

                var dbItems =
                    (await context.PhotoContents.Where(x => e.ContentIds.Contains(x.ContentId)).ToListAsync()).Select(
                        ListItemFromDbItem);

                await ThreadSwitcher.ResumeForegroundAsync();

                foreach (var loopUpdates in dbItems)
                {
                    var toUpdate = Items.SingleOrDefault(x => x.DbEntry.ContentId == loopUpdates.DbEntry.ContentId);
                    if (toUpdate == null)
                    {
                        Items.Add(loopUpdates);
                        continue;
                    }

                    toUpdate.DbEntry = loopUpdates.DbEntry;
                    toUpdate.SmallImageUrl = loopUpdates.SmallImageUrl;
                }
            }
        }

        private async Task FilterList()
        {
            if (Items == null || !Items.Any()) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            ((CollectionView) CollectionViewSource.GetDefaultView(Items)).Filter = o =>
            {
                if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

                var loweredString = UserFilterText.ToLower();

                if (!(o is PhotoListListItem pi)) return false;
                if ((pi.DbEntry.Title ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Tags ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Summary ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CreatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.LastUpdatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CameraMake ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CameraModel ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Aperture ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.FocalLength ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Lens ?? string.Empty).ToLower().Contains(loweredString)) return true;
                return false;
            };
        }

        public PhotoListListItem ListItemFromDbItem(PhotoContent content)
        {
            return new PhotoListListItem
            {
                DbEntry = content,
                SmallImageUrl = PictureAssetProcessing.ProcessPhotoDirectory(content).SmallPicture?.File.FullName
            };
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Connecting to DB");

            var db = await Db.Context();

            StatusContext.Progress("Getting Photo Db Entries");

            var dbItems = LoadRecentOnly
                ? db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn).Take(20).ToList()
                : db.PhotoContents.ToList();

            var listItems = new List<PhotoListListItem>();

            var totalCount = dbItems.Count;
            var currentLoop = 1;

            foreach (var loopItems in dbItems)
            {
                if (currentLoop == 1 || currentLoop % 25 == 0)
                    StatusContext.Progress($"Processing Photo Item {currentLoop} of {totalCount}");

                listItems.Add(ListItemFromDbItem(loopItems));

                currentLoop++;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Loading Display List of Photos");

            Items = new ObservableCollection<PhotoListListItem>(listItems);

            AllLoaded = !LoadRecentOnly;

            SortDescending = true;
            await SortList("CreatedOn");
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task SortList(string sortColumn)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            _lastSortColumn = sortColumn;

            var collectionView = ((CollectionView) CollectionViewSource.GetDefaultView(Items));
            collectionView.SortDescriptions.Clear();

            if (string.IsNullOrWhiteSpace(sortColumn)) return;
            collectionView.SortDescriptions.Add(new SortDescription($"DbEntry.{sortColumn}",
                SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending));
        }
    }
}