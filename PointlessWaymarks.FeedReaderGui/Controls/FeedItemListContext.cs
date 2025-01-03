using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Data;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FeedItemListContext : IStandardListWithContext<FeedItemListListItem>
{
    public bool AutoMarkRead { get; set; } = true;
    public required FeedQueries ContextDb { get; init; }
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public string DisplayBasicAuthPassword { get; set; } = string.Empty;
    public string DisplayBasicAuthUsername { get; set; } = string.Empty;
    public string DisplayUrl { get; set; } = string.Empty;
    public WebViewMessenger FeedDisplayPage { get; set; } = new();
    public List<Guid> FeedList { get; set; } = [];
    public Func<Task<OneOf<Success<byte[]>, Error<string>>>>? ItemRssViewScreenshotFunction { get; set; }
    public Func<Task<OneOf<Success<byte[]>, Error<string>>>>? ItemWebViewScreenshotFunction { get; set; }
    public required ColumnSortControlContext ListSort { get; init; }
    public FeedItemListListItem? SelectedItem { get; set; }
    public List<FeedItemListListItem> SelectedItems { get; set; } = [];
    public bool ShowUnread { get; set; }
    public string UserAddFeedInput { get; set; } = string.Empty;
    public string UserFilterText { get; set; } = string.Empty;

    public FeedItemListListItem? SelectedListItem()
    {
        return SelectedItem;
    }

    public List<FeedItemListListItem> SelectedListItems()
    {
        return SelectedItems;
    }

    public required StatusControlContext StatusContext { get; set; }
    public required ObservableCollection<FeedItemListListItem> Items { get; init; }

    [NonBlockingCommand]
    public async Task ClearReadItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toRemove = Items.Where(x => x.DbItem.MarkedRead).ToList();

        if (!toRemove.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var x in toRemove) Items.Remove(x);
    }

    private async Task ComposeFeedDisplayHtml(FeedItemListListItem? item)
    {
        if (item == null)
        {
            await FeedDisplayPage.SetupDocumentWithMinimalCss("""<p>"No Valid Item?"</p>""", "Nothing...");
            return;
        }

        var htmlBody = $"""
                        <h3><a href="{item.DbItem.Link}">{item.DbItem.Title.HtmlEncode()}</a></h3>
                        <h4>{item.DbReaderFeed.Name.HtmlEncode()}</h4>
                        <hr />
                        <p>{item.DbItem.Description}</p>
                        <hr />
                        {item.DbItem.Content}
                        <hr />
                        <ul>
                         <li>Link: <a href="{item.DbItem.Link}">{item.DbItem.Link}</a></li>
                         <li>Author: {item.DbItem.Author.HtmlEncode()}</li>
                         <li>Created On: {item.DbItem.CreatedOn:F}</li>
                         <li>Publishing Date: {item.DbItem.PublishingDate:F}</li>
                         <li>Feed Item Id: {item.DbItem.FeedItemId.HtmlEncode()}</li>
                         <li>Id: {item.DbItem.Id}</li>
                         <li>Persistent Id: {item.DbItem.PersistentId}</li>
                        </ul>
                        """;

        await FeedDisplayPage.SetupDocumentWithMinimalCss(htmlBody, item.DbItem.Title ?? "No Title?");
    }

    public static async Task<FeedItemListContext> CreateInstance(StatusControlContext statusContext, string dbFile,
        List<Guid>? feedList = null, bool showUnread = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryItemsList = new ObservableCollection<FeedItemListListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var feedQueries = new FeedQueries() { DbFileFullName = dbFile };

        var newContext = new FeedItemListContext
        {
            Items = factoryItemsList,
            StatusContext = statusContext,
            FeedList = feedList ?? [],
            ShowUnread = showUnread,
            ContextDb = feedQueries,
            ListSort = new ColumnSortControlContext
            {
                Items =
                [
                    new ColumnSortControlSortItem
                    {
                        DisplayName = "Posted",
                        ColumnName = "DbItem.PublishingDate",
                        Order = 1,
                        DefaultSortDirection = ListSortDirection.Descending
                    },

                    new ColumnSortControlSortItem
                    {
                        DisplayName = "Item Name",
                        ColumnName = "DbItem.Title",
                        DefaultSortDirection = ListSortDirection.Descending
                    },

                    new ColumnSortControlSortItem
                    {
                        DisplayName = "Feed Name",
                        ColumnName = "DbReaderFeed.Name",
                        DefaultSortDirection = ListSortDirection.Ascending
                    },

                    new ColumnSortControlSortItem
                    {
                        DisplayName = "Item Author",
                        ColumnName = "DbItem.Author",
                        DefaultSortDirection = ListSortDirection.Ascending
                    }
                ]
            }
        };

        await newContext.Setup();

        return newContext;
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message.ToString());

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
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await ContextDb.GetInstance();
        var currentFeed =
            await db.Feeds.SingleOrDefaultAsync(x => x.PersistentId == listItem.DbReaderFeed.PersistentId);

        if (currentFeed == null)
        {
            await StatusContext.ToastError("Feed Not Found?!?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await FeedEditorWindow.CreateInstance(currentFeed, ContextDb.DbFileFullName);
        window.PositionWindowAndShow();
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItem]
    public async Task FeedEditorForSelectedItem()
    {
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

            return toFilter.DbReaderFeed.Name.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbReaderFeed.Tags.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbReaderFeed.Note.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || (toFilter.DbItem.Title?.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase) ??
                       false)
                   || (toFilter.DbItem.Author?.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase) ??
                       false)
                   || (toFilter.DbItem.Link?.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase) ??
                       false)
                   || (toFilter.DbItem.Description?.Contains(cleanedFilterText,
                       StringComparison.OrdinalIgnoreCase) ?? false)
                ;
        };
    }


    [BlockingCommand]
    private async Task ItemWebViewScreenshot()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ItemWebViewScreenshotFunction == null)
        {
            await StatusContext.ToastError("Screenshot function not available...");
            return;
        }

        var screenshotResult = await ItemWebViewScreenshotFunction();

        if (screenshotResult.IsT0)
            await WebViewToJpg.SaveByteArrayAsJpg(screenshotResult.AsT0.Value, string.Empty, StatusContext);
        else
            await StatusContext.ToastError(screenshotResult.AsT1.Value);
    }

    [BlockingCommand]
    private async Task ItemRssViewScreenshot()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ItemRssViewScreenshotFunction == null)
        {
            await StatusContext.ToastError("Screenshot function not available...");
            return;
        }

        var screenshotResult = await ItemRssViewScreenshotFunction();

        if (screenshotResult.IsT0)
            await WebViewToJpg.SaveByteArrayAsJpg(screenshotResult.AsT0.Value, string.Empty, StatusContext);
        else
            await StatusContext.ToastError(screenshotResult.AsT1.Value);
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task MarkdownLinksForSelectedItems()
    {
        var clipboardBlock = new StringBuilder();

        foreach (var loopItems in SelectedListItems())
            clipboardBlock.AppendLine($"[{loopItems.DbItem.Title ?? "No Title"}]({loopItems.DbItem.Link})");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(clipboardBlock.ToString());
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task MarkSelectedRead()
    {
        await ContextDb.ItemRead(SelectedItems.Select(x => x.DbItem.PersistentId).ToList(), true);
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task MarkSelectedUnRead()
    {
        await ContextDb.ItemRead(SelectedItems.Select(x => x.DbItem.PersistentId).ToList(), false);
    }

    [BlockingCommand]
    public async Task NewFeedEditorFromUrl()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrEmpty(UserAddFeedInput))
        {
            await StatusContext.ToastWarning("Feed to Add is Blank?");
            return;
        }

        var feedItem = await ContextDb.TryGetFeed(UserAddFeedInput, StatusContext.ProgressTracker());

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
            StatusContext.RunNonBlockingTask(FilterList);

        if (e.PropertyName == nameof(AutoMarkRead))
            StatusContext.RunNonBlockingTask(async () =>
            {
                try
                {
                    var settings = FeedReaderGuiSettingTools.ReadSettings();
                    settings.AutoMarkReadDefault = AutoMarkRead;
                    await FeedReaderGuiSettingTools.WriteSettings(settings);
                }
                catch (Exception)
                {
                    //Ignored
                }
            });

        if (e.PropertyName.Equals(nameof(SelectedItem)))
        {
            if (SelectedItem is { DbItem: { MarkedRead: false, KeepUnread: false } } && AutoMarkRead)
                StatusContext.RunNonBlockingTask(async () =>
                {
                    await ContextDb.ItemRead(SelectedItem.DbItem.PersistentId.AsList(), true);
                });

            StatusContext.RunNonBlockingTask(async () =>
            {
                try
                {
                    if (SelectedItem is not null && SelectedItem.DbReaderFeed.UseBasicAuth)
                    {
                        var credentials = await FeedReaderEncryption.DecryptBasicAuthCredentials(
                            SelectedItem.DbReaderFeed.BasicAuthUsername,
                            SelectedItem.DbReaderFeed.BasicAuthPassword,
                            ContextDb.DbFileFullName);
                        DisplayBasicAuthUsername = credentials.username;
                        DisplayBasicAuthPassword = credentials.password;
                    }
                    else
                    {
                        DisplayBasicAuthUsername = string.Empty;
                        DisplayBasicAuthPassword = string.Empty;
                    }

                    StatusContext.RunNonBlockingTask(async () =>
                        await ComposeFeedDisplayHtml(SelectedItem));
                    DisplayUrl = string.IsNullOrWhiteSpace(SelectedItem?.DbItem.Link)
                        ? "about:blank"
                        : SelectedItem.DbItem.Link;
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Error With Display URL in the FeedItemListContext");
                    await FeedDisplayPage.SetupDocumentWithMinimalCss($"""<h2>Exception</h2><p>{exception}</p>""",
                        "Error");
                    DisplayUrl = "about:blank";
                }
            });
        }
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItem]
    public async Task OpenSelectedItemInBrowser()
    {
        if (string.IsNullOrWhiteSpace(SelectedItem?.DbItem.Link))
        {
            await StatusContext.ToastWarning("Feed Item has no Link?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Process.Start(new ProcessStartInfo(SelectedItem.DbItem.Link) { UseShellExecute = true });
    }

    private async Task ProcessDataUpdateNotification(InterProcessDataNotification interProcessUpdateNotification)
    {
        if (interProcessUpdateNotification.ContentType == DataNotificationContentType.FeedItem)
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

        if (interProcessUpdateNotification.ContentType == DataNotificationContentType.Feed)
        {
            var feedsToUpdate = FeedList.Any()
                ? FeedList.Intersect(interProcessUpdateNotification.ContentIds).ToList()
                : Items.Select(x => x.DbReaderFeed.PersistentId).ToList();
            if (!feedsToUpdate.Any()) return;

            var toUpdate = Items.Where(x => feedsToUpdate.Contains(x.DbItem.FeedPersistentId))
                .Select(x => x.DbItem.PersistentId).ToList();
            await UpdateFeedItems(toUpdate);
        }
    }

    [BlockingCommand]
    public async Task RefreshFeedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var errors = FeedList is { Count: > 0 }
            ? await ContextDb.UpdateFeeds(FeedList, StatusContext.ProgressTracker())
            : await ContextDb.UpdateFeeds(StatusContext.ProgressTracker());
        foreach (var loopError in errors) await StatusContext.ToastError(loopError);
    }


    [BlockingCommand]
    private async Task RssViewScreenshot()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ItemRssViewScreenshotFunction == null)
        {
            await StatusContext.ToastError("Screenshot function not available...");
            return;
        }

        var screenshotResult = await ItemRssViewScreenshotFunction();

        if (screenshotResult.IsT0)
            await WebViewToJpg.SaveByteArrayAsJpg(screenshotResult.AsT0.Value, string.Empty, StatusContext);
        else
            await StatusContext.ToastError(screenshotResult.AsT1.Value);
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task SaveSelectedItems()
    {
        await ContextDb.SaveFeedItems(SelectedItems.Select(x => x.DbItem.PersistentId).ToList());
    }

    public async Task Setup()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        BuildCommands();

        var db = await ContextDb.GetInstance();

        var initialItemFilter = db.FeedItems.Where(x => x.MarkedRead == ShowUnread);
        if (FeedList.Any()) initialItemFilter = initialItemFilter.Where(x => FeedList.Contains(x.FeedPersistentId));

        var initialItems = await initialItemFilter.OrderByDescending(x => x.PublishingDate)
            .ThenBy(x => x.Title).ToListAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopItems in initialItems)
            Items.Add(new FeedItemListListItem
            {
                DbItem = loopItems, DbReaderFeed = db.Feeds.Single(x => x.PersistentId == loopItems.FeedPersistentId)
            });

        await ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
        await FilterList();

        ListSort.SortUpdated += (_, list) =>
            StatusContext.RunNonBlockingTask(() => ListContextSortHelpers.SortList(list, Items));

        PropertyChanged += OnPropertyChanged;
        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;

        StatusContext.RunNonBlockingTask(async () => { await RefreshFeedItems(); });

        JobManager.Initialize();

        JobManager.AddJob(
            async () =>
            {
                try
                {
                    await RefreshFeedItems();
                }
                catch (Exception e)
                {
                    Log.ForContext("ignored exception", e.ToString())
                        .Verbose("Error in Feed Item List Background Refresh (Ignored)");
                }
            },
            s => s.ToRunEvery(2).Hours()
        );
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task TitleAndUrlForSelectedItems()
    {
        var clipboardBlock = new StringBuilder();

        foreach (var loopItems in SelectedListItems())
            clipboardBlock.AppendLine($"{loopItems.DbItem.Title ?? "(No Title)"} - {loopItems.DbItem.Link}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(clipboardBlock.ToString());
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task TitlesForSelectedItems()
    {
        var clipboardBlock = new StringBuilder();

        foreach (var loopItems in SelectedListItems())
            clipboardBlock.AppendLine($"{loopItems.DbItem.Title ?? "(No Title)"}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(clipboardBlock.ToString());
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItem]
    public async Task ToggleKeepUnread(FeedItemListListItem? listItem)
    {
        await ContextDb.ItemKeepUnreadToggle(listItem!.DbItem.PersistentId.AsList(), StatusContext.ProgressTracker());
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task ToggleSelectedKeepUnRead()
    {
        await ContextDb.ItemKeepUnreadToggle(SelectedItems.Select(x => x.DbItem.PersistentId).ToList(),
            StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task TryAddFeed()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrEmpty(UserAddFeedInput))
        {
            await StatusContext.ToastWarning("Feed to Add is Blank?");
            return;
        }

        var result = await ContextDb.TryAddFeed(UserAddFeedInput, StatusContext.ProgressTracker());

        result.Switch(_ => StatusContext.ToastSuccess($"Added Feed for {UserAddFeedInput}"),
            error => StatusContext.ToastError(error.Value));

        UserAddFeedInput = string.Empty;
    }

    public async Task UpdateFeedItems(List<Guid> toUpdate)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await ContextDb.GetInstance();

        foreach (var loopContentIds in toUpdate)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var listItem =
                Items.SingleOrDefault(x => x.DbItem.PersistentId == loopContentIds);
            var dbFeedItem = db.FeedItems.SingleOrDefault(x =>
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

            var dbFeed = db.Feeds.SingleOrDefault(x =>
                x.PersistentId == dbFeedItem.FeedPersistentId);

            //If the Feed is not in the Db remove the item from the Gui
            //Display if it exists - the assumption here is that the data
            //is either in the process of changing or needs a cleanup - an
            //absent feed means all FeedItems should have been purged.
            if (dbFeed == null)
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
                listItem.DbItem = dbFeedItem;
                listItem.DbReaderFeed = dbFeed;
            }
            else
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Add(new FeedItemListListItem { DbReaderFeed = dbFeed, DbItem = dbFeedItem });
            }
        }

        if (SelectedItem != null && toUpdate.Contains(SelectedItem.DbItem.PersistentId))
            if (SelectedItem.DbItem is { KeepUnread: false, MarkedRead: false } && AutoMarkRead)
                StatusContext.RunNonBlockingTask(async () =>
                    await ContextDb.ItemRead(SelectedItem.DbItem.PersistentId.AsList(), true));
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task UrlsForSelectedItems()
    {
        var clipboardBlock = new StringBuilder();

        foreach (var loopItems in SelectedListItems()) clipboardBlock.AppendLine($"{loopItems.DbItem.Link}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(clipboardBlock.ToString());
    }
}