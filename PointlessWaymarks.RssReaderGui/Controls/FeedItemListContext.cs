using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.RssReaderData;
using PointlessWaymarks.RssReaderData.Models;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.RssReaderGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FeedItemListContext
{
    public bool AutoMarkRead { get; set; } = true;
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public string DisplayUrl { get; set; } = string.Empty;

    public List<Guid> FeedList { get; set; } = new List<Guid>();
    public required ObservableCollection<FeedItemListListItem> Items { get; set; }
    public required ColumnSortControlContext ListSort { get; set; }
    public FeedItemListListItem? SelectedItem { get; set; }
    public List<FeedItemListListItem> SelectedItems { get; set; } = new();
    public required StatusControlContext StatusContext { get; set; }
    public string UserAddFeedInput { get; set; } = string.Empty;
    public string UserFilterText { get; set; } = string.Empty;

    [NonBlockingCommand]
    public async Task ClearReadItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toRemove = Items.Where(x => x.DbItem.MarkedRead).ToList();

        if (!toRemove.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var x in toRemove) Items.Remove(x);
    }

    public static async Task<FeedItemListContext> CreateInstance(StatusControlContext statusContext,
        List<Guid>? feedList = null)
    {
        var settings = RssReaderGuiSettingTools.ReadSettings();

        if (string.IsNullOrWhiteSpace(settings.DatabaseFile) || !File.Exists(settings.DatabaseFile))
        {
            var newDb = UniqueFileTools.UniqueFile(
                FileLocationHelpers.DefaultStorageDirectory(), "PointlessWaymarks-RssReader.db");
            settings.DatabaseFile = newDb!.FullName;

            await FeedContext.CreateInstanceWithEnsureCreated(newDb.FullName);

            await RssReaderGuiSettingTools.WriteSettings(settings);
        }

        FeedContext.CurrentDatabaseFileName = settings.DatabaseFile;

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryItemsList = new ObservableCollection<FeedItemListListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new FeedItemListContext
        {
            Items = factoryItemsList,
            StatusContext = statusContext,
            FeedList = feedList ?? new(),
            ListSort = new ColumnSortControlContext
            {
                Items = new List<ColumnSortControlSortItem>
                {
                    new()
                    {
                        DisplayName = "Posted",
                        ColumnName = "DbItem.FeedPublishingDate",
                        Order = 1,
                        DefaultSortDirection = ListSortDirection.Descending
                    },
                    new()
                    {
                        DisplayName = "Item Name",
                        ColumnName = "DbItem.FeedTitle",
                        DefaultSortDirection = ListSortDirection.Descending
                    },
                    new()
                    {
                        DisplayName = "Feed Name",
                        ColumnName = "DbFeed.Name",
                        DefaultSortDirection = ListSortDirection.Ascending
                    },
                    new()
                    {
                        DisplayName = "Item Author",
                        ColumnName = "DbItem.FeedAuthor",
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
    public async Task FeedEditorForFeedItem(FeedItemListListItem? listItem)
    {
        if (listItem?.DbItem == null) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await FeedContext.CreateInstance();
        var currentFeed = await db.Feeds.SingleOrDefaultAsync(x => x.PersistentId == listItem.DbFeed.PersistentId);

        if (currentFeed == null)
        {
            StatusContext.ToastError("Feed Not Found?!?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await FeedEditorWindow.CreateInstance(currentFeed);
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

        await FeedEditorForFeedItem(SelectedItem);
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
            if (o is not FeedItemListListItem toFilter) return false;

            return toFilter.DbFeed.Name.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbFeed.Tags.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbFeed.Note.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || (toFilter.DbItem.FeedTitle?.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase) ??
                       false)
                   || (toFilter.DbItem.FeedAuthor?.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase) ??
                       false)
                   || (toFilter.DbItem.FeedLink?.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase) ??
                       false)
                   || (toFilter.DbItem.FeedDescription?.Contains(cleanedFilterText,
                       StringComparison.OrdinalIgnoreCase) ?? false)
                ;
        };
    }

    [NonBlockingCommand]
    public async Task MarkKeepUnread(FeedItemListListItem? listItem)
    {
        if (listItem?.DbItem == null) return;

        await FeedQueries.ItemKeepUnreadToggle(listItem.DbItem.PersistentId.AsList());
    }

    [BlockingCommand]
    public async Task MarkSelectedRead()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastWarning("Nothing Selected to Mark Read?");
            return;
        }

        await FeedQueries.ItemRead(SelectedItems.Select(x => x.DbItem.PersistentId).ToList(), true);
    }

    [BlockingCommand]
    public async Task MarkSelectedUnRead()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastWarning("Nothing Selected to Mark Read?");
            return;
        }

        await FeedQueries.ItemRead(SelectedItems.Select(x => x.DbItem.PersistentId).ToList(), false);
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

        if (e.PropertyName.Equals(nameof(SelectedItem)))
        {
            if (SelectedItem is { DbItem: { MarkedRead: false, KeepUnread: false } } && AutoMarkRead)
            {
                StatusContext.RunFireAndForgetNonBlockingTask(async () =>
                {
                    await FeedQueries.ItemRead(SelectedItem.DbItem.PersistentId.AsList(), true);
                });
            }

            DisplayUrl = string.IsNullOrWhiteSpace(SelectedItem?.DbItem.FeedLink)
                ? "about:blank"
                : SelectedItem.DbItem.FeedLink;
        }
    }

    private async Task ProcessDataUpdateNotification(InterProcessDataNotification interProcessUpdateNotification)
    {
        if (interProcessUpdateNotification.ContentType == DataNotificationContentType.RssItem)
        {
            if (interProcessUpdateNotification.UpdateType == DataNotificationUpdateType.Delete)
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                var toRemove = Items
                    .Where(x => interProcessUpdateNotification.ContentIds.Contains(x.DbItem.PersistentId)).ToList();
                toRemove.ForEach(x => Items.Remove(x));
                return;
            }

            if (interProcessUpdateNotification.UpdateType is DataNotificationUpdateType.Update
                or DataNotificationUpdateType.New)
                await UpdateFeedItems(interProcessUpdateNotification.ContentIds);
        }
    }

    [BlockingCommand]
    public async Task RefreshFeedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var errors = FeedList is { Count: > 0 }
            ? await FeedQueries.UpdateFeeds(StatusContext.ProgressTracker())
            : await FeedQueries.UpdateFeeds(FeedList, StatusContext.ProgressTracker());
        foreach (var loopError in errors) StatusContext.ToastError(loopError);
    }

    public async Task Setup()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        BuildCommands();

        var db = await FeedContext.CreateInstance();

        var initialItemFilter = db.FeedItems.Where(x => !x.MarkedRead);
        if (FeedList.Any()) initialItemFilter = initialItemFilter.Where(x => FeedList.Contains(x.FeedPersistentId));

        var initialItems = await initialItemFilter.OrderByDescending(x => x.FeedPublishingDate)
            .ThenBy(x => x.FeedTitle).ToListAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopItems in initialItems)
            Items.Add(new FeedItemListListItem
            {
                DbItem = loopItems, DbFeed = db.Feeds.Single(x => x.PersistentId == loopItems.FeedPersistentId)
            });

        ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
        await FilterList();

        ListSort.SortUpdated += (_, list) =>
            Dispatcher.CurrentDispatcher.Invoke(() => { ListContextSortHelpers.SortList(list, Items); });

        PropertyChanged += OnPropertyChanged;
        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;

        await RefreshFeedItems();
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

    public async Task UpdateFeedItems(List<Guid> toUpdate)
    {
        ThreadSwitcher.ResumeBackgroundAsync();

        var db = await FeedContext.CreateInstance();

        foreach (var loopContentIds in toUpdate)
        {
            ThreadSwitcher.ResumeBackgroundAsync();

            var listItem =
                Items.SingleOrDefault(x => x.DbItem.PersistentId == loopContentIds);
            var dbRssItem = db.FeedItems.SingleOrDefault(x =>
                x.PersistentId == loopContentIds);

            //If there is no database item remove it if it exists in the Gui Items and 
            //continue
            if (dbRssItem == null)
            {
                if (listItem != null)
                {
                    await ThreadSwitcher.ResumeForegroundAsync();
                    Items.Remove(listItem);
                }

                continue;
            }

            var dbFeedItem = db.Feeds.SingleOrDefault(x =>
                x.PersistentId == dbRssItem.FeedPersistentId);

            //If the Feed is not in the Db remove the item from the Gui
            //Display if it exists - the assumption here is that the data
            //is either in the process of changing or needs a cleanup - an
            //absent feed means all FeedItems should have been purged.
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
                listItem.DbItem = dbRssItem;
                listItem.DbFeed = dbFeedItem;
            }
            else
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Add(new FeedItemListListItem { DbFeed = dbFeedItem, DbItem = dbRssItem });
            }
        }

        if (SelectedItem != null && toUpdate.Contains(SelectedItem.DbItem.PersistentId))
            if (SelectedItem.DbItem is { KeepUnread: false, MarkedRead: false } && AutoMarkRead)
                StatusContext.RunFireAndForgetNonBlockingTask(async () =>
                    await FeedQueries.ItemRead(SelectedItem.DbItem.PersistentId.AsList(), true));
    }
}