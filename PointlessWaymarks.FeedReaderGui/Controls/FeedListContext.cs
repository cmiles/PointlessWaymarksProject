using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;
using WinRT;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FeedListContext
{
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<FeedListListItem> Items { get; set; }
    public required ColumnSortControlContext ListSort { get; set; }
    public FeedListListItem? SelectedItem { get; set; }
    public List<FeedListListItem> SelectedItems { get; set; } = new List<FeedListListItem>();
    public required StatusControlContext StatusContext { get; set; }

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

        await FeedQueries.ArchiveFeed(SelectedItem.DbFeed.PersistentId, StatusContext.ProgressTracker());
    }

    public static async Task<FeedListContext> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newItems = new ObservableCollection<FeedListListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new FeedListContext
        {
            StatusContext = statusContext,
            Items = newItems,
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

    [NonBlockingCommand]
    public async Task FeedEditorForFeed(FeedListListItem? listItem)
    {
        if (listItem?.DbFeed == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await FeedEditorWindow.CreateInstance(listItem.DbFeed);
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

            return toFilter.DbFeed.Name.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbFeed.Tags.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbFeed.Note.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbFeed.Url.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase);
        };
    }

    [NonBlockingCommand]
    public async Task MarkAllRead(FeedListListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem?.DbFeed == null) return;

        await FeedQueries.FeedAllItemsRead(listItem.DbFeed.PersistentId, true);
    }

    [NonBlockingCommand]
    public async Task MarkAllUnRead(FeedListListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem?.DbFeed == null) return;

        await FeedQueries.FeedAllItemsRead(listItem.DbFeed.PersistentId, false);
    }

    [BlockingCommand]
    public async Task NewFeedEditorFromUrl()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var feedItem = await FeedQueries.TryGetFeed(UserAddFeedInput, StatusContext.ProgressTracker());

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await FeedEditorWindow.CreateInstance(feedItem);

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
                    .Where(x => interProcessUpdateNotification.ContentIds.Contains(x.DbFeed.PersistentId)).ToList();
                toRemove.ForEach(x => Items.Remove(x));
                return;
            }

            if (interProcessUpdateNotification.UpdateType is DataNotificationUpdateType.Update
                or DataNotificationUpdateType.New)
                await UpdateFeedListItems(interProcessUpdateNotification.ContentIds);
        }

        if (interProcessUpdateNotification.ContentType == DataNotificationContentType.FeedItem)
        {
            StatusContext.RunFireAndForgetBlockingTask(async () =>
                await UpdateReadCount(interProcessUpdateNotification.ContentIds));
        }
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
            await FeedQueries.UpdateFeeds(SelectedItem.DbFeed.PersistentId.AsList(), StatusContext.ProgressTracker());
        foreach (var loopError in errors) StatusContext.ToastError(loopError);
    }

    public async Task Setup()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        BuildCommands();

        var db = await FeedContext.CreateInstance();

        var initialItems = (await db.Feeds.ToListAsync()).Select(x => new FeedListListItem { DbFeed = x }).ToList();

        var feedCounts = await db.FeedItems.GroupBy(x => x.FeedPersistentId)
            .Select(x => new { FeedPersistentId = x.Key, AllFeedItemsCount = x.Count() }).ToListAsync();

        var unReadFeedCounts = await db.FeedItems.Where(x => !x.MarkedRead).GroupBy(x => x.FeedPersistentId)
            .Select(x => new { FeedPersistentId = x.Key, UnreadItemsCount = x.Count() }).ToListAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopItem in initialItems)
        {
            loopItem.ItemsCount = feedCounts.SingleOrDefault(x => x.FeedPersistentId == loopItem.DbFeed.PersistentId)
                ?.AllFeedItemsCount ?? 0;
            loopItem.UnreadItemsCount = unReadFeedCounts
                .SingleOrDefault(x => x.FeedPersistentId == loopItem.DbFeed.PersistentId)?.UnreadItemsCount ?? 0;
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

        var db = await FeedContext.CreateInstance();

        foreach (var loopContentIds in toUpdate)
        {
            ThreadSwitcher.ResumeBackgroundAsync();

            var listItem =
                Items.SingleOrDefault(x => x.DbFeed.PersistentId == loopContentIds);
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
                listItem.DbFeed = dbFeedItem;
            }
            else
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Add(new FeedListListItem { DbFeed = dbFeedItem });
            }
        }
    }

    public async Task UpdateReadCount(List<Guid> changedItemGuid)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await FeedContext.CreateInstance();

        var feedIds = await db.FeedItems.Where(x => changedItemGuid.Contains(x.PersistentId))
            .GroupBy(x => x.FeedPersistentId)
            .Select(x => x.Key).ToListAsync();

        foreach (var loopFeedId in feedIds)
        {
            var totalItems = await db.FeedItems.CountAsync(x => x.FeedPersistentId == loopFeedId);
            var unReadItems = await db.FeedItems.CountAsync(x => x.FeedPersistentId == loopFeedId && !x.MarkedRead);

            var item = Items.SingleOrDefault(x => x.DbFeed.PersistentId == loopFeedId);

            if (item == null) return;

            item.ItemsCount = totalItems;
            item.UnreadItemsCount = unReadItems;
        }
    }

    [NonBlockingCommand]
    public async Task ViewFeedItems(FeedListListItem? listItem)
    {
        if (listItem?.DbFeed == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await FeedItemListWindow.CreateInstance(listItem.DbFeed.PersistentId.AsList());
        window.PositionWindowAndShow();
    }

    [NonBlockingCommand]
    public async Task ViewFeedItemsForSelectedItem()
    {
        if (SelectedItem == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        await ViewFeedItems(SelectedItem);
    }
}