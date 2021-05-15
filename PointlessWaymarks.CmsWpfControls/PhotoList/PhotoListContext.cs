using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.PhotoList
{
    public class PhotoListContext : INotifyPropertyChanged
    {
        public enum PhotoListLoadMode
        {
            Recent,
            All,
            ReportQuery
        }

        private DataNotificationsWorkQueue _dataNotificationsProcessor;

        private ObservableCollection<PhotoListListItem> _items;
        private string _lastSortColumn = "CreatedOn";
        private ContentListSelected<PhotoListListItem> _listSelection;
        private PhotoListLoadMode _loadMode = PhotoListLoadMode.Recent;
        private Func<Task<List<PhotoContent>>> _reportGenerator;
        private bool _sortDescending = true;
        private Command<string> _sortListCommand;
        private StatusControlContext _statusContext;
        private Command _toggleListSortDirectionCommand;
        private Command _toggleLoadRecentLoadAllCommand;
        private string _userFilterText;
        private PhotoListItemActions _itemActions;

        public PhotoListContext(StatusControlContext statusContext, PhotoListLoadMode photoListLoadMode)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            ItemActions = new PhotoListItemActions(StatusContext);
            
            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};

            SortListCommand = StatusContext.RunNonBlockingTaskCommand<string>(SortList);
            ToggleListSortDirectionCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            {
                SortDescending = !SortDescending;
                await SortList(_lastSortColumn);
            });

            ToggleLoadRecentLoadAllCommand = new Command(() =>
            {
                if (LoadMode == PhotoListLoadMode.All)
                {
                    LoadMode = PhotoListLoadMode.Recent;
                    StatusContext.RunBlockingTask(LoadData);
                }
                else if (LoadMode == PhotoListLoadMode.Recent)
                {
                    LoadMode = PhotoListLoadMode.All;
                    StatusContext.RunBlockingTask(LoadData);
                }
            });

            LoadMode = photoListLoadMode;
        }


        public DataNotificationsWorkQueue DataNotificationsProcessor
        {
            get => _dataNotificationsProcessor;
            set
            {
                if (Equals(value, _dataNotificationsProcessor)) return;
                _dataNotificationsProcessor = value;
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


        public ContentListSelected<PhotoListListItem> ListSelection
        {
            get => _listSelection;
            set
            {
                if (Equals(value, _listSelection)) return;
                _listSelection = value;
                OnPropertyChanged();
            }
        }

        public PhotoListLoadMode LoadMode
        {
            get => _loadMode;
            set
            {
                if (value == _loadMode) return;
                _loadMode = value;
                OnPropertyChanged();
            }
        }


        public Func<Task<List<PhotoContent>>> ReportGenerator
        {
            get => _reportGenerator;
            set
            {
                if (Equals(value, _reportGenerator)) return;
                _reportGenerator = value;
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

        public Command ToggleLoadRecentLoadAllCommand
        {
            get => _toggleLoadRecentLoadAllCommand;
            set
            {
                if (Equals(value, _toggleLoadRecentLoadAllCommand)) return;
                _toggleLoadRecentLoadAllCommand = value;
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


        private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
        {
            var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

            if (translatedMessage.HasError)
            {
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                    translatedMessage.ErrorNote, StatusContext.StatusControlContextId);
                return;
            }

            if (translatedMessage.ContentType != DataNotificationContentType.Photo) return;

            await ThreadSwitcher.ResumeBackgroundAsync();

            if (translatedMessage.UpdateType == DataNotificationUpdateType.Delete)
            {
                var toRemove = Items.Where(x => translatedMessage.ContentIds.Contains(x.DbEntry.ContentId)).ToList();

                await ThreadSwitcher.ResumeForegroundAsync();

                toRemove.ForEach(x => Items.Remove(x));

                return;
            }

            var context = await Db.Context();

            var dbItems =
                (await context.PhotoContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Select(content => PhotoListItemActions.ListItemFromDbItem(content, ItemActions)).ToList();

            if (!dbItems.Any()) return;

            var listItems = Items.Where(x => translatedMessage.ContentIds.Contains(x.DbEntry.ContentId)).ToList();

            foreach (var loopItems in dbItems)
            {
                var existingItems = listItems.Where(x => x.DbEntry.ContentId == loopItems.DbEntry.ContentId).ToList();

                if (existingItems.Count > 1)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    foreach (var loopDelete in existingItems.Skip(1).ToList()) Items.Remove(loopDelete);

                    await ThreadSwitcher.ResumeBackgroundAsync();
                }

                var existingItem = existingItems.FirstOrDefault();

                if (existingItem == null)
                {
                    if (LoadMode == PhotoListLoadMode.ReportQuery) continue;

                    await ThreadSwitcher.ResumeForegroundAsync();

                    Items.Add(loopItems);

                    await ThreadSwitcher.ResumeBackgroundAsync();

                    continue;
                }

                if (translatedMessage.UpdateType == DataNotificationUpdateType.Update)
                    existingItem.DbEntry = loopItems.DbEntry;

                existingItem.SmallImageUrl = PhotoListItemActions.GetSmallImageUrl(existingItem.DbEntry);
            }

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
        }

        private async Task FilterList()
        {
            if (Items == null || !Items.Any()) return;

            await ThreadSwitcher.ResumeForegroundAsync();

            ((CollectionView) CollectionViewSource.GetDefaultView(Items)).Filter = o =>
            {
                if (string.IsNullOrWhiteSpace(UserFilterText)) return true;

                var loweredString = UserFilterText.ToLower();

#pragma warning disable IDE0083 // Use pattern matching
                if (!(o is PhotoListListItem pi)) return false;
#pragma warning restore IDE0083 // Use pattern matching
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

        public PhotoListItemActions ItemActions
        {
            get => _itemActions;
            set
            {
                if (Equals(value, _itemActions)) return;
                _itemActions = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

            ListSelection = await ContentListSelected<PhotoListListItem>.CreateInstance(StatusContext);

            StatusContext.Progress("Connecting to DB");

            var db = await Db.Context();

            StatusContext.Progress("Getting Photo Db Entries");

            var dbItems = LoadMode switch
            {
                PhotoListLoadMode.Recent => db.PhotoContents.OrderByDescending(x => x.LastUpdatedOn ?? x.CreatedOn)
                    .Take(20).ToList(),
                PhotoListLoadMode.All => db.PhotoContents.ToList(),
                PhotoListLoadMode.ReportQuery => ReportGenerator == null
                    ? new List<PhotoContent>()
                    : await ReportGenerator(),
                _ => throw new ArgumentOutOfRangeException()
            };

            var listItems = new List<PhotoListListItem>();

            var totalCount = dbItems.Count;
            var currentLoop = 1;

            foreach (var loopItems in dbItems)
            {
                if (currentLoop == 1 || currentLoop % 25 == 0)
                    StatusContext.Progress($"Processing Photo Item {currentLoop} of {totalCount}");

                listItems.Add(PhotoListItemActions.ListItemFromDbItem(loopItems, ItemActions));

                currentLoop++;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Loading Display List of Photos");

            Items = new ObservableCollection<PhotoListListItem>(listItems);
            await SortList(_lastSortColumn);
            await FilterList();

            DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
        }

        private void OnDataNotificationReceived(object sender, TinyMessageReceivedEventArgs e)
        {
            DataNotificationsProcessor.Enqueue(e);
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        private static async Task RunReport(Func<Task<List<PhotoContent>>> toRun, string title)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var context = new PhotoListWithActionsContext(null, toRun);

            await ThreadSwitcher.ResumeForegroundAsync();

            var newWindow = new PhotoListWindow {PhotoListContext = context, WindowTitle = title};

            newWindow.PositionWindowAndShow();
        }

        private async Task SortList(string sortColumn)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            _lastSortColumn = sortColumn;

            var collectionView = (CollectionView) CollectionViewSource.GetDefaultView(Items);
            collectionView.SortDescriptions.Clear();

            if (string.IsNullOrWhiteSpace(sortColumn)) return;
            collectionView.SortDescriptions.Add(new SortDescription($"DbEntry.{sortColumn}",
                SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending));
        }
        
    }
}