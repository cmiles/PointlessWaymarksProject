using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ColumnSort;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.PostList
{
    public class PostListContext : INotifyPropertyChanged
    {
        private ObservableCollection<PostListListItem> _items;
        private ContentListSelected<PostListListItem> _listSelection;
        private ColumnSortControlContext _listSort;
        private Command<string> _sortListCommand;
        private StatusControlContext _statusContext;
        private Command _toggleListSortDirectionCommand;
        private string _userFilterText;
        private PostListItemActions _itemActions;

        public PostListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            ItemActions = new PostListItemActions(StatusContext);
            
            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};
            
            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);

            DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
        }

        public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }

        public ObservableCollection<PostListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public ContentListSelected<PostListListItem> ListSelection
        {
            get => _listSelection;
            set
            {
                if (Equals(value, _listSelection)) return;
                _listSelection = value;
                OnPropertyChanged();
            }
        }

        public ColumnSortControlContext ListSort
        {
            get => _listSort;
            set
            {
                if (Equals(value, _listSort)) return;
                _listSort = value;
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

        private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

            if (translatedMessage.HasError)
            {
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                    translatedMessage.ErrorNote, StatusContext.StatusControlContextId);
                return;
            }

            if (translatedMessage.ContentType == DataNotificationContentType.Post)
                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () =>
                    await PostDataNotificationReceived(translatedMessage));
            if (translatedMessage.ContentType != DataNotificationContentType.Post)
                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () =>
                    await PossibleMainImageUpdateDataNotificationReceived(translatedMessage));
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
                if (!(o is PostListListItem pi)) return false;
#pragma warning restore IDE0083 // Use pattern matching
                if ((pi.DbEntry.Title ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Tags ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Summary ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CreatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.LastUpdatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                return false;
            };
        }

        public PostListItemActions ItemActions
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

            ListSelection = await ContentListSelected<PostListListItem>.CreateInstance(StatusContext);

            StatusContext.Progress("Connecting to DB");

            var db = await Db.Context();

            StatusContext.Progress("Getting Post Db Entries");
            var dbItems = db.PostContents.OrderByDescending(x => x.CreatedOn).ToList();
            var listItems = new List<PostListListItem>();

            var totalCount = dbItems.Count;
            var currentLoop = 1;

            foreach (var loopItems in dbItems)
            {
                if (totalCount == 1 || totalCount % 10 == 0)
                    StatusContext.Progress($"Processing Post Item {currentLoop} of {totalCount}");

                listItems.Add(PostListItemActions.ListItemFromDbItem(loopItems, ItemActions));

                currentLoop++;
            }

            ListSort = new ColumnSortControlContext
            {
                Items = new List<ColumnSortControlSortItem>
                {
                    new()
                    {
                        DisplayName = "Latest Update",
                        ColumnName = "DbEntry.LatestUpdate",
                        Order = 1,
                        DefaultSortDirection = ListSortDirection.Descending
                    },
                    new()
                    {
                        DisplayName = "Title",
                        ColumnName = "DbEntry.Title",
                        DefaultSortDirection = ListSortDirection.Ascending
                    },
                    new()
                    {
                        DisplayName = "Created On",
                        ColumnName = "DbEntry.CreatedOn",
                        DefaultSortDirection = ListSortDirection.Descending
                    }
                }
            };

            ListSort.SortUpdated += (sender, list) =>
                Dispatcher.CurrentDispatcher.Invoke(() => { ListContextSortHelpers.SortList(list, Items); });
            ;

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Displaying Posts");

            Items = new ObservableCollection<PostListListItem>(listItems);

            ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
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

        private async Task PossibleMainImageUpdateDataNotificationReceived(
            InterProcessDataNotification translatedMessage)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var toUpdate = Items.Where(x =>
                    x.DbEntry.MainPicture != null && translatedMessage.ContentIds.Contains(x.DbEntry.MainPicture.Value))
                .ToList();

            toUpdate.ForEach(x =>
            {
                x.SmallImageUrl = null;
                x.SmallImageUrl = PostListItemActions.GetSmallImageUrl(x.DbEntry);
            });
        }

        private async Task PostDataNotificationReceived(InterProcessDataNotification translatedMessage)
        {
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
                (await context.PostContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Select(x => PostListItemActions.ListItemFromDbItem(x, ItemActions)).ToList();

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
                    await ThreadSwitcher.ResumeForegroundAsync();

                    Items.Add(loopItems);

                    await ThreadSwitcher.ResumeBackgroundAsync();

                    continue;
                }

                if (translatedMessage.UpdateType == DataNotificationUpdateType.Update)
                    existingItem.DbEntry = loopItems.DbEntry;

                existingItem.SmallImageUrl = PostListItemActions.GetSmallImageUrl(existingItem.DbEntry);
            }

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
        }
    }
}