using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsData.Pictures;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.FileList
{
    public class FileListContext : INotifyPropertyChanged
    {
        private ObservableRangeCollection<FileListListItem> _items;
        private string _lastSortColumn;
        private Command<FileListListItem> _openFileCommand;
        private List<FileListListItem> _selectedItems;
        private bool _sortDescending;
        private Command<string> _sortListCommand;
        private StatusControlContext _statusContext;
        private Command _toggleListSortDirectionCommand;
        private string _userFilterText;

        public FileListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);

            DataNotifications.FileContentDataNotificationEvent += DataNotificationsOnContentDataNotificationEvent;
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

        public Command<FileListListItem> OpenFileCommand
        {
            get => _openFileCommand;
            set
            {
                if (Equals(value, _openFileCommand)) return;
                _openFileCommand = value;
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

                StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(FilterList);
            }
        }

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

                Items.RemoveRange(toRemove);
            }

            if (e.UpdateType == DataNotificationUpdateType.New)
            {
                var context = await Db.Context();

                var dbItems =
                    (await context.FileContents.Where(x => e.ContentIds.Contains(x.ContentId)).ToListAsync()).Select(
                        ListItemFromDbItem);

                await ThreadSwitcher.ResumeForegroundAsync();

                Items.AddRange(dbItems);
            }

            if (e.UpdateType == DataNotificationUpdateType.Update)
            {
                var context = await Db.Context();

                var dbItems =
                    (await context.FileContents.Where(x => e.ContentIds.Contains(x.ContentId)).ToListAsync()).Select(
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

                if (!(o is FileListListItem pi)) return false;
                if ((pi.DbEntry.Title ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Tags ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Summary ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CreatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.LastUpdatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.OriginalFileName ?? string.Empty).ToLower().Contains(loweredString)) return true;
                return false;
            };
        }


        public FileListListItem ListItemFromDbItem(FileContent content)
        {
            var newItem = new FileListListItem {DbEntry = content};

            if (content.MainPicture != null)
                newItem.SmallImageUrl = PictureAssetProcessing.ProcessPictureDirectory(content.MainPicture.Value)
                    .SmallPicture?.File.FullName;

            return newItem;
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            SortListCommand = new Command<string>(x => StatusContext.RunNonBlockingTask(() => SortList(x)));
            ToggleListSortDirectionCommand = new Command(() => StatusContext.RunNonBlockingTask(async () =>
            {
                SortDescending = !SortDescending;
                await SortList(_lastSortColumn);
            }));
            OpenFileCommand = new Command<FileListListItem>(x => StatusContext.RunNonBlockingTask(() => OpenFile(x)));

            StatusContext.Progress("Connecting to DB");

            var db = await Db.Context();

            StatusContext.Progress("Getting File Db Entries");
            var dbItems = db.FileContents.ToList();
            var listItems = new List<FileListListItem>();

            var totalCount = dbItems.Count;
            var currentLoop = 1;

            foreach (var loopItems in dbItems)
            {
                if (totalCount == 1 || totalCount % 10 == 0)
                    StatusContext.Progress($"Processing File Item {currentLoop} of {totalCount}");

                listItems.Add(ListItemFromDbItem(loopItems));

                currentLoop++;
            }

            StatusContext.Progress("Displaying Files");

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableRangeCollection<FileListListItem>(listItems);

            SortDescending = true;
            await SortList("CreatedOn");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OpenFile(FileListListItem listItem)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (listItem == null)
            {
                StatusContext.ToastError("Nothing Items to Open?");
                return;
            }

            if (string.IsNullOrWhiteSpace(listItem.DbEntry.OriginalFileName))
            {
                StatusContext.ToastError("No File?");
                return;
            }

            var toOpen = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentFile(listItem.DbEntry);

            if (!toOpen.Exists)
            {
                StatusContext.ToastError("File doesn't exist?");
                return;
            }

            var url = toOpen.FullName;

            var ps = new ProcessStartInfo(url) {UseShellExecute = true, Verb = "open"};
            Process.Start(ps);
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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}