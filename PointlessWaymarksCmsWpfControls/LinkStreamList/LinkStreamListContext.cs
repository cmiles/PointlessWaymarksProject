using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using HtmlTableHelper;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers;
using MvvmHelpers.Commands;
using pinboard.net;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.HtmlViewer;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.LinkStreamList
{
    public class LinkStreamListContext : INotifyPropertyChanged
    {
        private Command<string> _copyUrlCommand;
        private ObservableRangeCollection<LinkStreamListListItem> _items;
        private string _lastSortColumn;
        private Command _listSelectedLinksNotOnPinboardCommand;
        private Command<string> _openUrlCommand;
        private List<LinkStreamListListItem> _selectedItems;
        private bool _sortDescending;
        private Command<string> _sortListCommand;
        private StatusControlContext _statusContext;
        private Command _toggleListSortDirectionCommand;
        private string _userFilterText;

        public LinkStreamListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
            DataNotifications.LinkStreamContentDataNotificationEvent += DataNotificationsOnContentDataNotificationEvent;
        }

        public Command<string> CopyUrlCommand
        {
            get => _copyUrlCommand;
            set
            {
                if (Equals(value, _copyUrlCommand)) return;
                _copyUrlCommand = value;
                OnPropertyChanged();
            }
        }

        public ObservableRangeCollection<LinkStreamListListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public Command ListSelectedLinksNotOnPinboardCommand
        {
            get => _listSelectedLinksNotOnPinboardCommand;
            set
            {
                if (Equals(value, _listSelectedLinksNotOnPinboardCommand)) return;
                _listSelectedLinksNotOnPinboardCommand = value;
                OnPropertyChanged();
            }
        }

        public Command<string> OpenUrlCommand
        {
            get => _openUrlCommand;
            set
            {
                if (Equals(value, _openUrlCommand)) return;
                _openUrlCommand = value;
                OnPropertyChanged();
            }
        }


        public List<LinkStreamListListItem> SelectedItems
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
                    (await context.LinkStreams.Where(x => e.ContentIds.Contains(x.ContentId)).ToListAsync()).Select(
                        ListItemFromDbItem);

                await ThreadSwitcher.ResumeForegroundAsync();

                Items.AddRange(dbItems);
            }

            if (e.UpdateType == DataNotificationUpdateType.Update)
            {
                var context = await Db.Context();

                var dbItems =
                    (await context.LinkStreams.Where(x => e.ContentIds.Contains(x.ContentId)).ToListAsync()).Select(
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

                if (!(o is LinkStreamListListItem pi)) return false;
                if ((pi.DbEntry.Tags ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Comments ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Title ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Site ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Author ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.Description ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.CreatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                if ((pi.DbEntry.LastUpdatedBy ?? string.Empty).ToLower().Contains(loweredString)) return true;
                return false;
            };
        }


        public LinkStreamListListItem ListItemFromDbItem(LinkStream content)
        {
            var newItem = new LinkStreamListListItem {DbEntry = content};

            return newItem;
        }

        private async Task ListSelectedLinksNotOnPinboard(IProgress<string> progress)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().PinboardApiToken))
            {
                progress?.Report("No Pinboard Api Token... Can't check Pinboard.");
                return;
            }

            var selected = SelectedItems.ToList();

            if (!selected.Any())
            {
                progress?.Report("Nothing Selected?");
                return;
            }

            progress?.Report($"Found {selected.Count} items to check.");


            using var pb = new PinboardAPI(UserSettingsSingleton.CurrentSettings().PinboardApiToken);

            var notFoundList = new List<LinkStream>();

            foreach (var loopSelected in selected)
            {
                if (string.IsNullOrWhiteSpace(loopSelected.DbEntry.Url))
                {
                    notFoundList.Add(loopSelected.DbEntry);
                    progress?.Report(
                        $"Link titled {loopSelected.DbEntry.Title} created on {loopSelected.DbEntry.CreatedOn:d} added because of blank URL...");
                    continue;
                }

                var matches = await pb.Posts.Get(null, null, loopSelected.DbEntry.Url, null);

                if (!matches.Posts.Any())
                {
                    progress?.Report(
                        $"Not Found Link titled {loopSelected.DbEntry.Title} created on {loopSelected.DbEntry.CreatedOn:d}");
                    notFoundList.Add(loopSelected.DbEntry);
                }
                else
                {
                    progress?.Report(
                        $"Found Link titled {loopSelected.DbEntry.Title} created on {loopSelected.DbEntry.CreatedOn:d}");
                }
            }

            if (!notFoundList.Any())
            {
                await StatusContext.ShowMessageWithOkButton("Pinboard Match Complete",
                    $"Found a match on Pinboard for all {selected.Count} Selected links.");
                return;
            }

            progress?.Report($"Building table of {notFoundList.Count} items not found on Pinboard");

            var projectedNotFound = notFoundList.Select(x => new
            {
                x.Title,
                x.Url,
                x.CreatedBy,
                x.CreatedOn,
                x.LastUpdatedBy,
                x.LastUpdatedOn
            }).ToHtmlTable();

            await ThreadSwitcher.ResumeForegroundAsync();

            var htmlReportWindow = new HtmlViewerWindow(projectedNotFound);
            htmlReportWindow.Show();
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
            OpenUrlCommand = new Command<string>(x => StatusContext.RunNonBlockingTask(() => OpenUrl(x)));
            CopyUrlCommand = new Command<string>(x => StatusContext.RunNonBlockingTask(async () =>
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                Clipboard.SetText(x);

                StatusContext.ToastSuccess($"To Clipboard {x}");
            }));
            ListSelectedLinksNotOnPinboardCommand = new Command(x =>
                StatusContext.RunBlockingTask(async () =>
                    await ListSelectedLinksNotOnPinboard(StatusContext.ProgressTracker())));

            StatusContext.Progress("Connecting to DB");

            var db = await Db.Context();

            StatusContext.Progress("Getting Link Db Entries");
            var dbItems = db.LinkStreams.ToList();
            var listItems = new List<LinkStreamListListItem>();

            var totalCount = dbItems.Count;
            var currentLoop = 1;

            foreach (var loopItems in dbItems)
            {
                if (totalCount == 1 || totalCount % 10 == 0)
                    StatusContext.Progress($"Processing Link Item {currentLoop} of {totalCount}");

                listItems.Add(ListItemFromDbItem(loopItems));

                currentLoop++;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            StatusContext.Progress("Displaying Links");

            Items = new ObservableRangeCollection<LinkStreamListListItem>(listItems);

            SortDescending = true;
            await SortList("CreatedOn");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OpenUrl(string url)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(url))
            {
                StatusContext.ToastError("Link is blank?");
                return;
            }

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