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
using System.Windows.Input;
using HtmlTableHelper;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MvvmHelpers.Commands;
using pinboard.net;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.HtmlViewer;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.LinkList
{
    public class LinkListContext : INotifyPropertyChanged
    {
        private Command<string> _copyUrlCommand;
        private Command<LinkContent> _editContentCommand;
        private ObservableCollection<LinkListListItem> _items;
        private string _lastSortColumn;

        private ObservableCollection<CommandBinding> _listBoxAppCommandBindings = new();

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

            SortListCommand = StatusContext.RunNonBlockingTaskCommand<string>(SortList);
            EditContentCommand = StatusContext.RunNonBlockingTaskCommand<LinkContent>(EditContent);
            ToggleListSortDirectionCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            {
                SortDescending = !SortDescending;
                await SortList(_lastSortColumn);
            });
            OpenUrlCommand = StatusContext.RunNonBlockingTaskCommand<string>(OpenUrl);
            CopyUrlCommand = StatusContext.RunNonBlockingTaskCommand<string>(async x =>
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                Clipboard.SetText(x);

                StatusContext.ToastSuccess($"To Clipboard {x}");
            });
            ListSelectedLinksNotOnPinboardCommand = StatusContext.RunBlockingTaskCommand(async () =>
                await ListSelectedLinksNotOnPinboard(StatusContext.ProgressTracker()));

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

        public Command<LinkContent> EditContentCommand
        {
            get => _editContentCommand;
            set
            {
                if (Equals(value, _editContentCommand)) return;
                _editContentCommand = value;
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

        public ObservableCollection<CommandBinding> ListBoxAppCommandBindings
        {
            get => _listBoxAppCommandBindings;
            set
            {
                if (Equals(value, _listBoxAppCommandBindings)) return;
                _listBoxAppCommandBindings = value;
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
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                    translatedMessage.ErrorNote, StatusContext.StatusControlContextId);
                return;
            }

            if (translatedMessage.ContentType != DataNotificationContentType.Link) return;

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
                (await context.LinkContents.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
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
            }

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(FilterList);
        }

        private async Task EditContent(LinkContent content)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (content == null) return;

            var context = await Db.Context();

            var refreshedData = context.LinkContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            if (refreshedData == null)
                StatusContext.ToastError($"{content.Title} is no longer active in the database? Can not edit - " +
                                         "look for a historic version...");

            await ThreadSwitcher.ResumeForegroundAsync();

            var newContentWindow = new LinkContentEditorWindow(refreshedData);

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
                if (!(o is LinkListListItem pi)) return false;
#pragma warning restore IDE0083 // Use pattern matching
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

        public static LinkListListItem ListItemFromDbItem(LinkContent content)
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

                var matches = await pb.Posts.Get(null, null, loopSelected.DbEntry.Url);

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
            htmlReportWindow.PositionWindowAndShow();
        }

        public async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

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
            await FilterList();

            DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
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

            var collectionView = (CollectionView) CollectionViewSource.GetDefaultView(Items);
            collectionView.SortDescriptions.Clear();

            if (string.IsNullOrWhiteSpace(sortColumn)) return;
            collectionView.SortDescriptions.Add(new SortDescription($"DbEntry.{sortColumn}",
                SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending));
        }
    }
}