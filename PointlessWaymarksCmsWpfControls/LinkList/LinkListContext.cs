using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using MvvmHelpers.Commands;
using pinboard.net;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.HtmlViewer;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.WpfHtml;
using TinyIpc.Messaging;

namespace PointlessWaymarksCmsWpfControls.LinkList
{
    public class LinkListContext : INotifyPropertyChanged
    {
        private Command<string> _copyUrlCommand;
        private ObservableCollection<LinkListListItem> _items;
        private string _lastSortColumn;
        private Command _listSelectedLinksNotOnPinboardCommand;
        private Command<string> _openUrlCommand;
        private List<LinkListListItem> _selectedItems;
        private bool _sortDescending;
        private Command<string> _sortListCommand;
        private StatusControlContext _statusContext;
        private Command _toggleListSortDirectionCommand;
        private string _userFilterText;

        public LinkListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

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

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
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

        public ObservableCollection<LinkListListItem> Items
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


        public List<LinkListListItem> SelectedItems
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

        private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
        {
            var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

            if (translatedMessage.HasError)
            {
                await EventLogContext.TryWriteDiagnosticMessageToLog(
                    $"Data Notification Failure in PostListContext - {translatedMessage.ErrorNote}",
                    StatusContext.StatusControlContextId.ToString());
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
                (await context.LinkContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId)).ToListAsync())
                .Select(ListItemFromDbItem);

            var listItems = Items.Where(x => translatedMessage.ContentIds.Contains(x.DbEntry.ContentId)).ToList();

            foreach (var loopItems in dbItems)
            {
                var existingItem = listItems.SingleOrDefault(x => x.DbEntry.ContentId == loopItems.DbEntry.ContentId);

                if (existingItem == null)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    Items.Add(loopItems);

                    await ThreadSwitcher.ResumeBackgroundAsync();

                    continue;
                }

                if (translatedMessage.UpdateType == DataNotificationUpdateType.Update)
                    existingItem.DbEntry = loopItems.DbEntry;
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

                if (!(o is LinkListListItem pi)) return false;
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


        public LinkListListItem ListItemFromDbItem(LinkContent content)
        {
            var newItem = new LinkListListItem {DbEntry = content};

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

            var notFoundList = new List<LinkContent>();

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
            }).ToHtmlTable(new {@class = "pure-table pure-table-striped"});

            await ThreadSwitcher.ResumeForegroundAsync();

            var htmlReportWindow =
                new HtmlViewerWindow(
                    projectedNotFound.ToHtmlDocumentWithPureCss("Links Not In Pinboard", string.Empty));
            htmlReportWindow.Show();
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DataNotifications.DataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

            StatusContext.Progress("Connecting to DB");

            var db = await Db.Context();

            StatusContext.Progress("Getting Link Db Entries");
            var dbItems = db.LinkContents.ToList();
            var listItems = new List<LinkListListItem>();

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

            Items = new ObservableCollection<LinkListListItem>(listItems);

            SortDescending = true;

            await SortList("CreatedOn");

            DataNotifications.DataNotificationChannel().MessageReceived += OnDataNotificationReceived;
        }

        private void OnDataNotificationReceived(object sender, TinyMessageReceivedEventArgs e)
        {
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await DataNotificationReceived(e));
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
    }
}