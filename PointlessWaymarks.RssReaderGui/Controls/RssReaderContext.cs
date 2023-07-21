using System.Collections.ObjectModel;
using System.IO;
using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.RssReaderData;
using PointlessWaymarks.RssReaderData.Models;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.RssReaderGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class RssReaderContext
{
    public required ObservableCollection<RssReaderListItem> Items { get; set; }
    public RssReaderListItem? SelectedItem { get; set; }
    public List<RssReaderListItem> SelectedItems { get; set; } = new();

    public required StatusControlContext StatusContext { get; set; }

    public string UserAddFeedInput { get; set; } = string.Empty;

    public static async Task<RssReaderContext> CreateInstance(StatusControlContext statusContext)
    {
        var settings = RssReaderGuiSettingTools.ReadSettings();

        if (string.IsNullOrWhiteSpace(settings.DatabaseFile) || !File.Exists(settings.DatabaseFile))
        {
            var newDb = UniqueFileTools.UniqueFile(
                FileLocationHelpers.DefaultStorageDirectory(), "PointlessWaymarks-RssReader.db");
            settings.DatabaseFile = newDb!.FullName;

            await RssContext.CreateInstanceWithEnsureCreated(newDb.FullName);

            await RssReaderGuiSettingTools.WriteSettings(settings);
        }

        RssContext.CurrentDatabaseFileName = settings.DatabaseFile;

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryItemsList = new ObservableCollection<RssReaderListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new RssReaderContext
        {
            Items = factoryItemsList,
            StatusContext = statusContext
        };

        await newContext.Setup();

        return newContext;
    }

    [BlockingCommand]
    public async Task RefreshFeedItems()
    {
        var db = await RssContext.CreateInstance();

        var feeds = await db.RssFeeds.OrderBy(x => x.Name).ToListAsync();

        StatusContext.Progress($"Refreshing {feeds.Count} Feeds");

        var feedCounter = 0;
        var totalNewItemsCounter = 0;
        var totalExistingItemsCounter = 0;

        var toAddToGui = new List<RssItem>();

        foreach (var loopFeed in feeds)
        {
            feedCounter++;

            StatusContext.Progress(
                $"Feed {loopFeed.Name} - {feedCounter} of {feeds.Count} - {totalNewItemsCounter} New, {totalExistingItemsCounter} Existing");

            var currentFeed = await FeedReader.ReadAsync(loopFeed.Url);

            var currentFeedItems = currentFeed.Items.OrderByDescending(x => x.PublishingDate).ToList();

            StatusContext.Progress($"Feed {loopFeed.Name} - Found {currentFeedItems.Count} Feed Items to Process");

            var feedItemCounter = 0;
            var newItemCounter = 0;
            var existingItemCounter = 0;

            foreach (var loopFeedItem in currentFeedItems)
            {
                feedCounter++;

                if (feedCounter % 10 == 0)
                    StatusContext.Progress(
                        $"Feed {loopFeed.Name} - {feedItemCounter} of {currentFeedItems.Count} - {newItemCounter} New, {existingItemCounter} Existing");

                if (db.RssItems.Any(x => x.RssFeedPersistentId == loopFeed.PersistentId && x.FeedId == loopFeedItem.Id))
                {
                    existingItemCounter++;
                    continue;
                }

                newItemCounter++;

                var newFeedItem = new RssItem
                {
                    CreatedOn = DateTime.Now,
                    RssFeedPersistentId = loopFeed.PersistentId,
                    FeedContent = loopFeedItem.Content,
                    FeedId = loopFeedItem.Id,
                    FeedTitle = loopFeedItem.Title,
                    FeedAuthor = loopFeedItem.Author,
                    FeedPublishingDate = loopFeedItem.PublishingDate,
                    FeedDescription = loopFeedItem.Description,
                    FeedLink = loopFeedItem.Link
                };

                await db.RssItems.AddAsync(newFeedItem);

                await db.SaveChangesAsync();

                toAddToGui.Add(newFeedItem);
            }

            totalNewItemsCounter += newItemCounter;
            totalExistingItemsCounter += existingItemCounter;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopItem in toAddToGui) Items.Add(new RssReaderListItem { DbItem = loopItem });
    }

    public async Task Setup()
    {
        BuildCommands();

        var db = await RssContext.CreateInstance();

        var initialItems = await db.RssItems.Where(x => !x.MarkedRead).OrderByDescending(x => x.FeedPublishingDate)
            .ThenBy(x => x.FeedTitle).ToListAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopItems in initialItems) Items.Add(new RssReaderListItem { DbItem = loopItems });

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

        var db = await RssContext.CreateInstance();

        var cleanedUrl = UserAddFeedInput.Trim();

        if ((await db.RssFeeds.ToListAsync()).Any(x => x.Url.Equals(cleanedUrl, StringComparison.OrdinalIgnoreCase)))
        {
            StatusContext.ToastWarning("Feed already exists?");
            return;
        }

        Feed feedInfo;

        try
        {
            feedInfo = await FeedReader.ReadAsync(cleanedUrl);
        }
        catch (Exception e)
        {
            StatusContext.ToastWarning($"Problem Adding Feed - {e.Message}");
            return;
        }

        var newFeed = new RssFeed
        {
            PersistentId = Guid.NewGuid(),
            Name = feedInfo.Title,
            Url = cleanedUrl
        };

        await db.RssFeeds.AddAsync(newFeed);

        await db.SaveChangesAsync();

        await RefreshFeedItems();
    }
}