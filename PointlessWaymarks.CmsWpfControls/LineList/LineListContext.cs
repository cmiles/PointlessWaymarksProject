using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using GongSolutions.Wpf.DragDrop;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.CommonHtml;
using PointlessWaymarks.CmsWpfControls.LineContentEditor;
using PointlessWaymarks.CmsWpfControls.Status;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.Utility.ThreadSwitcher;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.LineList
{
    public class LineListContext : INotifyPropertyChanged, IDragSource
    {
        private Command<LineContent> _editContentCommand;
        private ObservableCollection<LineListListItem> _items;
        private string _lastSortColumn;
        private List<LineListListItem> _selectedItems;
        private bool _sortDescending;
        private Command<string> _sortListCommand;
        private StatusControlContext _statusContext;
        private Command _toggleListSortDirectionCommand;
        private string _userFilterText;

        public LineListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            DataNotificationsProcessor = new DataNotificationsWorkQueue {Processor = DataNotificationReceived};

            EditContentCommand = StatusContext.RunNonBlockingTaskCommand<LineContent>(EditContent);

            SortListCommand = StatusContext.RunNonBlockingTaskCommand<string>(SortList);
            ToggleListSortDirectionCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            {
                SortDescending = !SortDescending;
                await SortList(_lastSortColumn);
            });

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);

            DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
        }

        public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }

        public Command<LineContent> EditContentCommand
        {
            get => _editContentCommand;
            set
            {
                if (Equals(value, _editContentCommand)) return;
                _editContentCommand = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<LineListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public List<LineListListItem> SelectedItems
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

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            return true;
        }

        public void DragCancelled()
        {
        }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
        {
        }

        public void Dropped(IDropInfo dropInfo)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void StartDrag(IDragInfo dragInfo)
        {
            var items = dragInfo.SourceItems.OfType<LineListListItem>().Where(x => x.ContentId() != null).ToList();

            if (!items.Any())
            {
                dragInfo.Effects = DragDropEffects.None;
                return;
            }

            var wrapper = new SerializableContentCommonIdList
            {
                ContentIdList = items.Where(x => x.ContentId() != null).Select(x => x.ContentId().Value).ToList()
            };

            dragInfo.Data = wrapper;
            dragInfo.DataFormat = DataFormats.GetDataFormat(DataFormats.Serializable);
            dragInfo.Effects = dragInfo.Data != null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        public bool TryCatchOccurredException(Exception exception)
        {
            return false;
        }

        private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

            if (translatedMessage.HasError)
            {
                await EventLogContext.TryWriteDiagnosticMessageToLog(
                    $"Data Notification Failure in LineListContext - {translatedMessage.ErrorNote}",
                    StatusContext.StatusControlContextId.ToString());
                return;
            }

            if (translatedMessage.ContentType == DataNotificationContentType.Line)
                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () =>
                    await LineDataNotificationReceived(translatedMessage));
            if (translatedMessage.ContentType != DataNotificationContentType.Line)
                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () =>
                    await PossibleMainImageUpdateDataNotificationReceived(translatedMessage));
        }

        private async Task EditContent(LineContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.LineContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError($"{content.Title} is no longer active in the database? Can not edit - " +
                                         "look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new LineContentEditorWindow(refreshedData);

            newContentWindow.PositionWindowAndShow();

            await ThreadSwitcher.ResumeBackgroundAsync();
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
                if (!(o is LineListListItem pi)) return false;
#pragma warning restore IDE0083 // Use pattern matching
                if ((pi.DbEntry.Title ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Tags ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Summary ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CreatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.LastUpdatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                return false;
            };
        }

        public static string GetSmallImageUrl(LineContent content)
        {
            if (content?.MainPicture == null) return null;

            string smallImageUrl;

            try
            {
                smallImageUrl = PictureAssetProcessing.ProcessPictureDirectory(content.MainPicture.Value).SmallPicture
                    ?.File.FullName;
            }
            catch
            {
                smallImageUrl = null;
            }

            return smallImageUrl;
        }

        private async Task LineDataNotificationReceived(InterProcessDataNotification translatedMessage)
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
                (await context.LineContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
                    .ToListAsync()).Select(ListItemFromDbItem).ToList();

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

                existingItem.SmallImageUrl = GetSmallImageUrl(existingItem.DbEntry);
            }

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
        }

        public static LineListListItem ListItemFromDbItem(LineContent content)
        {
            return new() {DbEntry = content, SmallImageUrl = GetSmallImageUrl(content)};
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

            StatusContext.Progress("Connecting to DB");

            var db = await Db.Context();

            StatusContext.Progress("Getting Line Db Entries");
            var dbItems = db.LineContents.ToList();
            var listItems = new List<LineListListItem>();

            var totalCount = dbItems.Count;
            var currentLoop = 1;

            foreach (var loopItems in dbItems)
            {
                if (totalCount == 1 || totalCount % 10 == 0)
                    StatusContext.Progress($"Processing Line Item {currentLoop} of {totalCount}");

                listItems.Add(ListItemFromDbItem(loopItems));

                currentLoop++;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Displaying Lines");

            Items = new ObservableCollection<LineListListItem>(listItems);

            SortDescending = true;
            await SortList("CreatedOn");
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
                x.SmallImageUrl = GetSmallImageUrl(x.DbEntry);
            });
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