using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FeedListContext
{
    public required DbReference ContextDb { get; init; }
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<FeedListListItem> Items { get; init; }
    public required ColumnSortControlContext ListSort { get; init; }
    public FeedListListItem? SelectedItem { get; set; }
    public List<FeedListListItem> SelectedItems { get; set; } = new();
    public required StatusControlContext StatusContext { get; init; }
    public string UserAddFeedInput { get; set; } = string.Empty;
    public string UserFilterText { get; set; } = string.Empty;

    [BlockingCommand]
    public async Task ArchiveSelectedFeed()
    {
        if (SelectedItem == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        await FeedQueries.ArchiveFeed(SelectedItem.DbReaderFeed.PersistentId, StatusContext.ProgressTracker());
    }


    public static async Task<FeedListContext> CreateInstance(StatusControlContext statusContext, string dbFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newItems = new ObservableCollection<FeedListListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var dbReference = new DbReference { DbFileFullName = dbFile };

        var newContext = new FeedListContext
        {
            StatusContext = statusContext,
            Items = newItems,
            ContextDb = dbReference,
            ListSort = new ColumnSortControlContext
            {
                Items = new List<ColumnSortControlSortItem>
                {
                    new()
                    {
                        DisplayName = "Feed Name",
                        ColumnName = "DbFeed.Name",
                        Order = 1,
                        DefaultSortDirection = ListSortDirection.Ascending
                    },
                    new()
                    {
                        DisplayName = "Unread Count",
                        ColumnName = "UnreadItemsCount",
                        DefaultSortDirection = ListSortDirection.Descending
                    },
                    new()
                    {
                        DisplayName = "Last Successful Update",
                        ColumnName = "DbFeed.LastSuccessfulUpdate",
                        DefaultSortDirection = ListSortDirection.Descending
                    },
                    new()
                    {
                        DisplayName = "URL",
                        ColumnName = "DbFeed.Url",
                        DefaultSortDirection = ListSortDirection.Ascending
                    }
                }
            }
        };

        await newContext.Setup();

        return newContext;
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

        var toRun = translatedMessage.Match(ProcessDataUpdateNotification,
            x =>
            {
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}", x.ErrorMessage,
                    StatusContext.StatusControlContextId);
                return Task.CompletedTask;
            }
        );

        if (toRun is not null) await toRun;
    }

    [BlockingCommand]
    public async Task ExportSelectedUrlsToTextFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var feedsToExport = SelectedItems.Any() ? SelectedItems : Items.ToList();

        var urls = feedsToExport.Select(x => x.DbReaderFeed.Url).ToList();

        var urlListText = string.Join(Environment.NewLine, urls);

        await ThreadSwitcher.ResumeForegroundAsync();

        var saveDialog = new VistaSaveFileDialog
        {
            Title = "Save Feed URLs",
            Filter = "Text File|*.txt",
            DefaultExt = ".txt",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"FeedUrls_{DateTimeOffset.Now:yyyy-MM-dd}.txt",
            InitialDirectory = FeedReaderGuiSettingTools.GetLastDirectory().FullName,
            CheckPathExists = true,
            CheckFileExists = true,
            ValidateNames = true
        };

        var result = saveDialog.ShowDialog();

        if (result != true) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var saveFile = new FileInfo(saveDialog.FileName);

        await File.WriteAllTextAsync(saveFile.FullName, urlListText);
    }

    [NonBlockingCommand]
    public async Task FeedEditorForFeed(FeedListListItem? listItem)
    {
        if (listItem?.DbReaderFeed == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await FeedEditorWindow.CreateInstance(listItem.DbReaderFeed, ContextDb.DbFileFullName);
        window.PositionWindowAndShow();
    }

    [NonBlockingCommand]
    public async Task FeedEditorForSelectedItem()
    {
        if (SelectedItem == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        await FeedEditorForFeed(SelectedItem);
    }


    private async Task FilterList()
    {
        if (!Items.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        if (string.IsNullOrWhiteSpace(UserFilterText))
        {
            ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = _ => true;
            return;
        }

        var cleanedFilterText = UserFilterText.Trim();

        ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = o =>
        {
            if (o is not FeedListListItem toFilter) return false;

            return toFilter.DbReaderFeed.Name.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbReaderFeed.Tags.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbReaderFeed.Note.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbReaderFeed.Url.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase);
        };
    }

    [BlockingCommand]
    public async Task ImportUrlsFromTextFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var openDialog = new VistaOpenFileDialog
        {
            Title = "Open Link File",
            Filter = "Link File|*.txt",
            DefaultExt = ".txt",
            Multiselect = false,
            InitialDirectory = FeedReaderGuiSettingTools.GetLastDirectory().FullName,
            CheckPathExists = true,
            CheckFileExists = true,
            ValidateNames = true
        };

        var result = openDialog.ShowDialog();

        if (result != true) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        var openFile = new FileInfo(openDialog.FileName);

        var urlTextBlock = await File.ReadAllTextAsync(openFile.FullName);

        var urls = Regex.Split(urlTextBlock, "\r\n|\r|\n").ToList();
        urls.RemoveAll(string.IsNullOrWhiteSpace);

        var db = await ContextDb.GetInstance();
        var allUrls = await db.Feeds.Select(x => x.Url).AsNoTracking().ToListAsync();

        urls.RemoveAll(x => allUrls.Contains(x));

        if (!urls.Any())
        {
            StatusContext.ToastError("No New Links Found?");
            return;
        }

        foreach (var loopUrl in urls)
        {
            var addResult = await FeedQueries.TryAddFeed(loopUrl, StatusContext.ProgressTracker());
            addResult.Switch(_ => StatusContext.ToastSuccess($"Added {loopUrl}"),
                x => StatusContext.ToastError(x.Value));
        }
    }

    [NonBlockingCommand]
    public async Task MarkAllRead(FeedListListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem?.DbReaderFeed == null) return;

        await FeedQueries.FeedAllItemsRead(listItem.DbReaderFeed.PersistentId, true);
    }

    [NonBlockingCommand]
    public async Task MarkAllUnRead(FeedListListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem?.DbReaderFeed == null) return;

        await FeedQueries.FeedAllItemsRead(listItem.DbReaderFeed.PersistentId, false);
    }

    [BlockingCommand]
    public async Task NewFeedEditorFromUrl()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var feedItem = await FeedQueries.TryGetFeed(UserAddFeedInput, StatusContext.ProgressTracker());

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await FeedEditorWindow.CreateInstance(feedItem, ContextDb.DbFileFullName);

        window.PositionWindowAndShow();

        UserAddFeedInput = string.Empty;
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(UserFilterText))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    private async Task ProcessDataUpdateNotification(InterProcessDataNotification interProcessUpdateNotification)
    {
        if (interProcessUpdateNotification.ContentType == DataNotificationContentType.Feed)
        {
            if (interProcessUpdateNotification.UpdateType == DataNotificationUpdateType.Delete)
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                var toRemove = Items
                    .Where(x => interProcessUpdateNotification.ContentIds.Contains(x.DbReaderFeed.PersistentId))
                    .ToList();
                toRemove.ForEach(x => Items.Remove(x));
                return;
            }

            if (interProcessUpdateNotification.UpdateType is DataNotificationUpdateType.Update
                or DataNotificationUpdateType.New)
                await UpdateFeedListItems(interProcessUpdateNotification.ContentIds);
        }

        if (interProcessUpdateNotification.ContentType == DataNotificationContentType.FeedItem)
            StatusContext.RunFireAndForgetBlockingTask(async () =>
                await UpdateReadCount(interProcessUpdateNotification.ContentIds));
    }

    [NonBlockingCommand]
    public async Task RefreshFeeds()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var errors = await FeedQueries.UpdateFeeds(StatusContext.ProgressTracker());
        foreach (var loopError in errors) StatusContext.ToastError(loopError);
    }

    [NonBlockingCommand]
    public async Task RefreshSelectedFeed()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItem == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var errors =
            await FeedQueries.UpdateFeeds(SelectedItem.DbReaderFeed.PersistentId.AsList(),
                StatusContext.ProgressTracker());
        foreach (var loopError in errors) StatusContext.ToastError(loopError);
    }

    public async Task Setup()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        BuildCommands();

        var db = await ContextDb.GetInstance();

        var initialItems = (await db.Feeds.ToListAsync()).Select(x => new FeedListListItem { DbReaderFeed = x })
            .ToList();

        var feedCounts = await db.FeedItems.GroupBy(x => x.FeedPersistentId)
            .Select(x => new { FeedPersistentId = x.Key, AllFeedItemsCount = x.Count() }).ToListAsync();

        var unReadFeedCounts = await db.FeedItems.Where(x => !x.MarkedRead).GroupBy(x => x.FeedPersistentId)
            .Select(x => new { FeedPersistentId = x.Key, UnreadItemsCount = x.Count() }).ToListAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopItem in initialItems)
        {
            loopItem.ItemsCount = feedCounts
                .SingleOrDefault(x => x.FeedPersistentId == loopItem.DbReaderFeed.PersistentId)
                ?.AllFeedItemsCount ?? 0;
            loopItem.UnreadItemsCount = unReadFeedCounts
                .SingleOrDefault(x => x.FeedPersistentId == loopItem.DbReaderFeed.PersistentId)?.UnreadItemsCount ?? 0;
            Items.Add(loopItem);
        }

        ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);

        await FilterList();

        ListSort.SortUpdated += (_, list) =>
            Dispatcher.CurrentDispatcher.Invoke(() => { ListContextSortHelpers.SortList(list, Items); });

        PropertyChanged += OnPropertyChanged;

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    [BlockingCommand]
    public async Task TryAddFeed()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrEmpty(UserAddFeedInput))
        {
            StatusContext.ToastWarning("Feed to Add is Blank?");
            return;
        }

        var result = await FeedQueries.TryAddFeed(UserAddFeedInput, StatusContext.ProgressTracker());

        result.Switch(_ => StatusContext.ToastSuccess($"Added Feed for {UserAddFeedInput}"),
            error => StatusContext.ToastError(error.Value));

        UserAddFeedInput = string.Empty;
    }

    private async Task UpdateFeedListItems(List<Guid> toUpdate)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await ContextDb.GetInstance();

        foreach (var loopContentIds in toUpdate)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var listItem =
                Items.SingleOrDefault(x => x.DbReaderFeed.PersistentId == loopContentIds);
            var dbFeedItem = db.Feeds.SingleOrDefault(x =>
                x.PersistentId == loopContentIds);

            //If there is no database item remove it if it exists in the Gui Items and 
            //continue
            if (dbFeedItem == null)
            {
                if (listItem != null)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();
                    Items.Remove(listItem);
                }

                continue;
            }

            //Update the existing list item - if there isn't one fall thru
            //to the code below and add one.
            if (listItem != null)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                listItem.DbReaderFeed = dbFeedItem;
            }
            else
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Add(new FeedListListItem { DbReaderFeed = dbFeedItem });
            }
        }
    }

    public async Task UpdateReadCount(List<Guid> changedItemGuid)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await ContextDb.GetInstance();

        var feedIds = await db.FeedItems.Where(x => changedItemGuid.Contains(x.PersistentId))
            .GroupBy(x => x.FeedPersistentId)
            .Select(x => x.Key).ToListAsync();

        foreach (var loopFeedId in feedIds)
        {
            var totalItems = await db.FeedItems.CountAsync(x => x.FeedPersistentId == loopFeedId);
            var unReadItems = await db.FeedItems.CountAsync(x => x.FeedPersistentId == loopFeedId && !x.MarkedRead);

            var item = Items.SingleOrDefault(x => x.DbReaderFeed.PersistentId == loopFeedId);

            if (item == null) return;

            item.ItemsCount = totalItems;
            item.UnreadItemsCount = unReadItems;
        }
    }

    [NonBlockingCommand]
    public async Task ViewFeedItems(FeedListListItem? listItem, bool showReadItems)
    {
        if (listItem?.DbReaderFeed == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await FeedItemListWindow.CreateInstance(ContextDb.DbFileFullName,
            listItem.DbReaderFeed.PersistentId.AsList(), showReadItems);
        window.PositionWindowAndShow();
    }

    [NonBlockingCommand]
    public async Task ViewReadFeedItemsForSelectedItem()
    {
        if (SelectedItem == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        await ViewFeedItems(SelectedItem, true);
    }

    [NonBlockingCommand]
    public async Task ViewUnreadFeedItemsForSelectedItem()
    {
        if (SelectedItem == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        await ViewFeedItems(SelectedItem, false);
    }
}