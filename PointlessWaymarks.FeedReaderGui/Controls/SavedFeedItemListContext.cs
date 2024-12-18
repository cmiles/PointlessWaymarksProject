using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData;
using PointlessWaymarks.FeedReaderData.Models;
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

[GenerateStatusCommands]
[NotifyPropertyChanged]
public partial class SavedFeedItemListContext
{
    public required FeedQueries ContextDb { get; init; }
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public string DisplayUrl { get; set; } = string.Empty;
    public WebViewMessenger FeedDisplayPage { get; set; } = new();
    public List<Guid> FeedList { get; set; } = [];
    public Func<Task<OneOf<Success<byte[]>, Error<string>>>>? ItemRssViewScreenshotFunction { get; set; }
    public required ObservableCollection<SavedFeedItemListListItem> Items { get; init; }
    public Func<Task<OneOf<Success<byte[]>, Error<string>>>>? ItemWebViewScreenshotFunction { get; set; }
    public required ColumnSortControlContext ListSort { get; init; }
    public SavedFeedItemListListItem? SelectedItem { get; set; }
    public List<SavedFeedItemListListItem> SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; init; }
    public string UserFilterText { get; set; } = string.Empty;


    [NonBlockingCommand]
    public async Task ArchiveSelectedItems()
    {
        if (!SelectedItems.Any())
        {
            await StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        await ContextDb.ArchiveSavedItems(SelectedItems.Select(x => x.DbItem.PersistentId).ToList());
    }

    private async Task ComposeFeedDisplayHtml(SavedFeedItemListListItem? item)
    {
        if (item == null)
        {
            await FeedDisplayPage.SetupDocumentWithMinimalCss("""<p>"No Valid Item?"</p>""", "Nothing...");
            return;
        }

        var htmlBody = $"""
                        <h3><a href="{item.DbItem.Link}">{item.DbItem.Title.HtmlEncode()}</a></h3>
                        <h4>{item.DbReaderFeed?.Name.HtmlEncode() ?? "(No Feed Name)"}</h4>
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

    public static async Task<SavedFeedItemListContext> CreateInstance(StatusControlContext statusContext, string dbFile,
        List<Guid>? feedList = null, bool showUnread = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryItemsList = new ObservableCollection<SavedFeedItemListListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var feedQueries = new FeedQueries() { DbFileFullName = dbFile };

        var newContext = new SavedFeedItemListContext
        {
            Items = factoryItemsList,
            StatusContext = statusContext,
            FeedList = feedList ?? [],
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

    [BlockingCommand]
    public async Task DeleteSelected()
    {
        if (!SelectedItems.Any())
        {
            await StatusContext.ToastWarning("Nothing To Delete?");
            return;
        }

        var db = await ContextDb.GetInstance();

        foreach (var loopSelected in SelectedItems)
        {
            var newHistoric = new HistoricSavedFeedItem()
                { FeedPersistentId = loopSelected.DbItem.FeedPersistentId, FeedTitle = loopSelected.DbItem.FeedTitle };
            newHistoric.InjectFrom(loopSelected);

            db.HistoricSavedFeedItems.Add(newHistoric);
            db.SavedFeedItems.Remove(loopSelected.DbItem);

            await db.SaveChangesAsync();
        }
    }

    [NonBlockingCommand]
    public async Task FeedEditorForFeedItem(SavedFeedItemListListItem? listItem)
    {
        if (listItem?.DbItem == null)
        {
            await StatusContext.ToastWarning("This item isn't attached to an active feed...");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

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
    public async Task FeedEditorForSelectedItem()
    {
        if (SelectedItem == null)
        {
            await StatusContext.ToastWarning("Nothing Selected?");
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
            if (o is not SavedFeedItemListListItem toFilter) return false;

            return (toFilter.DbReaderFeed?.Name.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase) ??
                    false)
                   || (toFilter.DbReaderFeed?.Tags.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase) ??
                       false)
                   || (toFilter.DbReaderFeed?.Note.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase) ??
                       false)
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

    [NonBlockingCommand]
    public async Task MarkdownLinksForSelectedItems()
    {
        if (!SelectedItems.Any())
        {
            await StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var clipboardBlock = new StringBuilder();

        foreach (var loopItems in SelectedItems)
            clipboardBlock.AppendLine($"[{loopItems.DbItem.Title ?? "No Title"}]({loopItems.DbItem.Link})");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(clipboardBlock.ToString());
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
            StatusContext.RunFireAndForgetNonBlockingTask(async () => await ComposeFeedDisplayHtml(SelectedItem));

            DisplayUrl = string.IsNullOrWhiteSpace(SelectedItem?.DbItem.Link)
                ? "about:blank"
                : SelectedItem.DbItem.Link;
        }
    }

    [NonBlockingCommand]
    public async Task OpenSelectedItemInBrowser()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItem == null)
        {
            await StatusContext.ToastWarning("Feed to Add is Blank?");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedItem.DbItem.Link))
        {
            await StatusContext.ToastWarning("Feed Item has no Link?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Process.Start(new ProcessStartInfo(SelectedItem.DbItem.Link) { UseShellExecute = true });
    }

    private async Task ProcessDataUpdateNotification(InterProcessDataNotification interProcessUpdateNotification)
    {
        if (interProcessUpdateNotification.ContentType == DataNotificationContentType.SavedFeedItem)
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

    public async Task Setup()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        BuildCommands();

        var db = await ContextDb.GetInstance();

        var initialItemFilter = db.SavedFeedItems.AsQueryable();
        if (FeedList.Any()) initialItemFilter = initialItemFilter.Where(x => FeedList.Contains(x.FeedPersistentId));

        var initialItems = await initialItemFilter.OrderByDescending(x => x.PublishingDate)
            .ThenBy(x => x.Title).ToListAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopItems in initialItems)
            Items.Add(new SavedFeedItemListListItem
            {
                DbItem = loopItems,
                DbReaderFeed = db.Feeds.SingleOrDefault(x => x.PersistentId == loopItems.FeedPersistentId)
            });

        await ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
        await FilterList();

        ListSort.SortUpdated += (_, list) =>
            StatusContext.RunFireAndForgetNonBlockingTask(() => ListContextSortHelpers.SortList(list, Items));

        PropertyChanged += OnPropertyChanged;
        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    [NonBlockingCommand]
    public async Task TitleAndUrlForSelectedItems()
    {
        if (!SelectedItems.Any())
        {
            await StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var clipboardBlock = new StringBuilder();

        foreach (var loopItems in SelectedItems)
            clipboardBlock.AppendLine($"{loopItems.DbItem.Title ?? "(No Title)"} - {loopItems.DbItem.Link}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(clipboardBlock.ToString());
    }

    [NonBlockingCommand]
    public async Task TitlesForSelectedItems()
    {
        if (!SelectedItems.Any())
        {
            await StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var clipboardBlock = new StringBuilder();

        foreach (var loopItems in SelectedItems) clipboardBlock.AppendLine($"{loopItems.DbItem.Title ?? "(No Title)"}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(clipboardBlock.ToString());
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
            var dbFeedItem = db.SavedFeedItems.SingleOrDefault(x =>
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
                Items.Add(new SavedFeedItemListListItem { DbReaderFeed = dbFeed, DbItem = dbFeedItem });
            }
        }
    }

    [NonBlockingCommand]
    public async Task UrlsForSelectedItems()
    {
        if (!SelectedItems.Any())
        {
            await StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var clipboardBlock = new StringBuilder();

        foreach (var loopItems in SelectedItems) clipboardBlock.AppendLine($"{loopItems.DbItem.Link}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(clipboardBlock.ToString());
    }
}