using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReader;
using PointlessWaymarks.FeedReaderData.Models;
using Serilog;
using static PointlessWaymarks.FeedReader.Reader;

namespace PointlessWaymarks.FeedReaderData;

public class FeedQueries
{
    /// <summary>
    ///     Db file when the editor context is created - this allows the editor
    ///     to refer to the originating file even if another view has switched
    ///     to another db.
    /// </summary>
    public string DbFileFullName { get; init; } = string.Empty;

    public async Task ArchiveFeed(Guid feedPersistentId, IProgress<string> progress)
    {
        var db = await GetInstance();
        var toArchive = await db.Feeds.SingleOrDefaultAsync(x => x.PersistentId == feedPersistentId);

        if (toArchive is null)
        {
            progress.Report("No Db Items to archive...");
            return;
        }

        var items = await db.FeedItems.Where(x => x.FeedPersistentId == feedPersistentId).ToListAsync();

        progress.Report($"Archiving {items.Count} Feed Items");

        var counter = 0;

        foreach (var loopItems in items)
        {
            counter++;

            if (counter % 50 == 0) progress.Report($"Archiving {counter} of {items.Count} Feed Items");

            var historicFeedItem =
                await db.HistoricFeedItems.SingleOrDefaultAsync(x => x.PersistentId == loopItems.PersistentId);

            if (historicFeedItem is null)
            {
                historicFeedItem = new HistoricReaderFeedItem
                    { FeedPersistentId = loopItems.FeedPersistentId, PersistentId = loopItems.PersistentId };
                db.HistoricFeedItems.Add(historicFeedItem);
            }

            historicFeedItem.InjectFrom(loopItems);

            db.FeedItems.Remove(loopItems);

            await db.SaveChangesAsync();
        }

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.FeedItem,
            DataNotificationUpdateType.Delete, items.Select(x => x.PersistentId).ToList());

        var historicFeed = await db.HistoricFeeds.SingleOrDefaultAsync(x => x.PersistentId == toArchive.PersistentId);

        if (historicFeed == null)
        {
            historicFeed = new HistoricReaderFeed { PersistentId = toArchive.PersistentId };
            db.HistoricFeeds.Add(historicFeed);
        }

        historicFeed.InjectFrom(toArchive);

        db.Feeds.Remove(toArchive);

        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.Feed,
            DataNotificationUpdateType.Delete, historicFeed.PersistentId.AsList());
    }

    public async Task ArchiveSavedItems(List<Guid> toArchive)
    {
        if (!toArchive.Any()) return;

        var db = await GetInstance();

        var toArchiveDbItems = await db.SavedFeedItems.Where(x => toArchive.Contains(x.PersistentId)).ToListAsync();

        foreach (var loopItem in toArchiveDbItems)
        {
            var archiveItem = new HistoricSavedFeedItem();
            archiveItem.InjectFrom(loopItem);

            db.HistoricSavedFeedItems.Add(archiveItem);
            db.SavedFeedItems.Remove(loopItem);

            await db.SaveChangesAsync();

            DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.SavedFeedItem,
                DataNotificationUpdateType.Delete, archiveItem.PersistentId.AsList());
        }
    }

    public async Task AutoMarkAfterDayRefreshFeed(Guid feedPersistentId, IProgress<string> progress)
    {
        var db = await GetInstance();

        var feed = await db.Feeds.SingleOrDefaultAsync(x => x.PersistentId == feedPersistentId);

        if (feed is null)
        {
            progress.Report("Feed not found in database?");
            return;
        }

        if (feed.AutoMarkReadAfterDays is null or < 1 && feed.AutoMarkReadMoreThanItems is null or < 1) return;

        var candidateItems = await db.FeedItems
            .Where(x => x.FeedPersistentId == feedPersistentId && !x.MarkedRead && !x.KeepUnread).ToListAsync();

        var changeItems = new List<Guid>();

        foreach (var loopItem in candidateItems)
        {
            if (feed.AutoMarkReadAfterDays is > 0)
            {
                var compDate = loopItem.PublishingDate ?? loopItem.CreatedOn;
                if (DateTime.Now.Subtract(compDate).Days > feed.AutoMarkReadAfterDays)
                {
                    loopItem.MarkedRead = true;
                    changeItems.Add(loopItem.PersistentId);
                }
            }
        }

        await db.SaveChangesAsync();

        if (feed.AutoMarkReadMoreThanItems is > 0)
        {
            var unreadItems = await db.FeedItems
                .Where(x => x.FeedPersistentId == feedPersistentId && !x.MarkedRead && !x.KeepUnread).OrderByDescending(x => x.PublishingDate ?? x.CreatedOn).ToListAsync();

            if (unreadItems.Count > feed.AutoMarkReadMoreThanItems)
            {
                var beyondLimit = unreadItems.Skip(feed.AutoMarkReadMoreThanItems.Value).ToList();
                beyondLimit.ForEach(x =>
                {
                    x.MarkedRead = true;
                    changeItems.Add(x.PersistentId);
                });
                await db.SaveChangesAsync();
            }
        }
        
        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.FeedItem,
            DataNotificationUpdateType.Update, changeItems.Distinct().ToList());
    }

    public async Task AutoMarkAfterDayRefreshFeedItem(Guid persistentId, IProgress<string> progress)
    {
        var db = await GetInstance();

        var feedItem = await db.FeedItems.SingleOrDefaultAsync(x => x.PersistentId == persistentId);
        if (feedItem is null) return;
        if (feedItem.KeepUnread || feedItem.MarkedRead) return;

        var feed = await db.Feeds.SingleOrDefaultAsync(x => x.PersistentId == feedItem.FeedPersistentId);
        if (feed is null) return;

        if (feed.AutoMarkReadAfterDays is null or < 1) return;

        if (DateTime.Now.Subtract(feedItem.CreatedOn).Days > feed.AutoMarkReadAfterDays)
        {
            if (feedItem.PublishingDate is not null && DateTime.Now.Subtract(feedItem.PublishingDate.Value).Days <=
                feed.AutoMarkReadAfterDays)
                return;
            feedItem.MarkedRead = true;
            await db.SaveChangesAsync();

            DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.SavedFeedItem,
                DataNotificationUpdateType.Delete, feedItem.PersistentId.AsList());
        }
    }

    public async Task FeedAllItemsRead(Guid feedId, bool markRead)
    {
        var db = await GetInstance();

        var items = await db.FeedItems.Where(x => x.MarkedRead != markRead && x.FeedPersistentId == feedId)
            .OrderBy(x => x.Title)
            .Select(x => x.PersistentId).ToListAsync();

        await ItemRead(items, markRead);
    }

    /// <summary>
    ///     Use this to get the FeedContext Db with the DbFile value set when the
    ///     editor was created. This will ensure that even if the main/another view
    ///     has switched db files that the editor correctly refers to the originating
    ///     file.
    /// </summary>
    /// <returns></returns>
    public async Task<FeedContext> GetInstance()
    {
        return await FeedContext.CreateInstance(DbFileFullName);
    }

    public async Task ItemKeepUnreadToggle(List<Guid> itemIds, IProgress<string> progress)
    {
        var db = await GetInstance();

        var itemUpdateList = new List<Guid>();

        foreach (var loopIds in itemIds)
        {
            var item = db.FeedItems.SingleOrDefault(x => x.PersistentId == loopIds);

            if (item == null) return;

            item.KeepUnread = !item.KeepUnread;
            if (item.KeepUnread) item.MarkedRead = false;

            itemUpdateList.Add(item.PersistentId);
        }

        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.FeedItem,
            DataNotificationUpdateType.Update, itemUpdateList);

        foreach (var loopItem in itemUpdateList) await AutoMarkAfterDayRefreshFeedItem(loopItem, progress);
    }

    public async Task ItemRead(List<Guid> itemIds, bool markRead)
    {
        var db = await GetInstance();

        var updateList = new List<Guid>();

        foreach (var loopIds in itemIds)
        {
            var item = db.FeedItems.SingleOrDefault(x => x.PersistentId == loopIds);

            if (item == null) return;

            //Regardless of the actual request being process KeepUnread takes precedence...
            if (item.KeepUnread)
            {
                if (item.MarkedRead) continue;

                item.MarkedRead = false;
                updateList.Add(item.PersistentId);
                continue;
            }

            //If the data already represents the request continue
            if (item.MarkedRead == markRead) continue;

            //Updated
            item.MarkedRead = markRead;
            updateList.Add(item.PersistentId);
        }

        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.FeedItem,
            DataNotificationUpdateType.Update, updateList);
    }

    public async Task SaveFeedItems(List<Guid> toSave)
    {
        var db = await GetInstance();

        var toSaveDbItems = await db.FeedItems.Where(x => toSave.Contains(x.PersistentId)).ToListAsync();

        foreach (var loopItem in toSaveDbItems)
        {
            var loopItemFeedItem =
                await db.Feeds.SingleOrDefaultAsync(x => x.PersistentId == loopItem.FeedPersistentId);

            var toAdd = new SavedFeedItem()
            {
                Author = loopItem.Author,
                Content = loopItem.Content,
                CreatedOn = loopItem.CreatedOn,
                Description = loopItem.Description,
                FeedItemId = loopItem.FeedItemId,
                FeedItemPersistentId = loopItem.FeedPersistentId,
                FeedPersistentId = loopItem.FeedPersistentId,
                FeedTitle = loopItemFeedItem?.Name ?? string.Empty,
                Link = loopItem.Link,
                PersistentId = loopItem.PersistentId,
                PublishingDate = loopItem.PublishingDate,
                Title = loopItem.Title
            };

            db.SavedFeedItems.Add(toAdd);

            await db.SaveChangesAsync();

            DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.SavedFeedItem,
                DataNotificationUpdateType.New, toAdd.PersistentId.AsList());
        }
    }

    public async Task<OneOf<Success, Error<string>>> TryAddFeed(string url, IProgress<string> progress)
    {
        if (string.IsNullOrEmpty(url)) return new Error<string>("Feed to Add Url is Blank?");

        var db = await GetInstance();

        var cleanedUrl = url.Trim();

        if ((await db.Feeds.ToListAsync()).Any(x => x.Url.Equals(cleanedUrl, StringComparison.OrdinalIgnoreCase)))
            return new Error<string>("Feed already exists?");

        Feed feedInfo;

        try
        {
            feedInfo = await ReadAsync(cleanedUrl);
        }
        catch (Exception e)
        {
            return new Error<string>($"Problem Adding Feed - {e.Message}");
        }

        var newFeed = new ReaderFeed
        {
            Name = feedInfo.Title ?? new Uri(cleanedUrl).GetLeftPart(UriPartial.Authority),
            Url = cleanedUrl,
            FeedLastUpdatedDate = feedInfo.LastUpdatedDate
        };

        await db.Feeds.AddAsync(newFeed);

        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.Feed,
            DataNotificationUpdateType.New, newFeed.PersistentId.AsList());

        await UpdateFeeds(newFeed.PersistentId.AsList(), progress);

        return new Success();
    }

    /// <summary>
    ///     Sets up a new Feed object with as possible from the submitted URL - object is not saved to the database
    ///     and may be nothing other than a new() object if the feed can not be read.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public async Task<ReaderFeed> TryGetFeed(string url, IProgress<string> progress)
    {
        var toReturn = new ReaderFeed();

        if (string.IsNullOrEmpty(url)) return toReturn;

        var cleanedUrl = url.Trim();

        toReturn.Url = cleanedUrl;

        Feed? feedInfo;

        try
        {
            feedInfo = await ReadAsync(cleanedUrl);
        }
        catch (Exception)
        {
            return toReturn;
        }

        toReturn.Name = feedInfo.Title ?? new Uri(cleanedUrl).GetLeftPart(UriPartial.Authority);
        toReturn.FeedLastUpdatedDate = feedInfo.LastUpdatedDate;

        return toReturn;
    }

    public async Task<List<string>> UpdateFeeds(List<Guid> toUpdate, IProgress<string> progress)
    {
        var db = await GetInstance();

        var feeds = await db.Feeds.Where(x => toUpdate.Contains(x.PersistentId)).OrderBy(x => x.Name).ToListAsync();

        progress.Report($"Refreshing {feeds.Count} Feeds");

        var feedCounter = 0;
        var totalNewItemsCounter = 0;
        var totalExistingItemsCounter = 0;

        var returnErrors = new List<string>();

        foreach (var loopFeed in feeds)
        {
            var newFeedItems = new List<Guid>();

            feedCounter++;

            progress.Report(
                $"Feed {loopFeed.Name} - {feedCounter} of {feeds.Count} - {totalNewItemsCounter} New, {totalExistingItemsCounter} Existing");

            Feed? currentFeed;
            List<FeedItem> currentFeedItems;

            try
            {
                if (loopFeed.UseBasicAuth)
                {
                    var credentials = await FeedReaderEncryption.DecryptBasicAuthCredentials(loopFeed.BasicAuthUsername,
                        loopFeed.BasicAuthPassword, DbFileFullName);
                    currentFeed = await ReadAsync(loopFeed.Url, basicAuthUsername: credentials.username,
                        basicAuthPassword: credentials.password);
                }
                else
                {
                    currentFeed = await ReadAsync(loopFeed.Url);
                }
            }
            catch (Exception e)
            {
                returnErrors.Add($"{loopFeed.Url} - {e.Message}");
                Log.ForContext(nameof(loopFeed), loopFeed.SafeObjectDump())
                    .Error(e, "Error Updating Feed {feedUrl}", loopFeed.Url);
                continue;
            }

            try
            {
                currentFeedItems = currentFeed.Items.OrderByDescending(x => x.PublishingDate).ToList();
            }
            catch (Exception e)
            {
                returnErrors.Add($"{loopFeed.Url} - {e.Message}");
                Log.ForContext(nameof(loopFeed), loopFeed.SafeObjectDump())
                    .Error(e, "Error Updating Feed Items for {feedUrl}", loopFeed.Url);
                continue;
            }

            loopFeed.LastSuccessfulUpdate = DateTime.Now;
            await db.SaveChangesAsync();

            progress.Report($"Feed {loopFeed.Name} - Found {currentFeedItems.Count} Feed Items to Process");

            var feedItemCounter = 0;
            var newItemCounter = 0;
            var existingItemCounter = 0;

            foreach (var loopFeedItem in currentFeedItems)
            {
                feedItemCounter++;

                if (feedItemCounter % 10 == 0)
                    progress.Report(
                        $"Feed {loopFeed.Name} - {feedItemCounter} of {currentFeedItems.Count} - {newItemCounter} New, {existingItemCounter} Existing");

                var correctedFeedId = loopFeedItem.Id;
                if (string.IsNullOrWhiteSpace(correctedFeedId))
                    correctedFeedId = MD5.HashData(Encoding.UTF8.GetBytes(loopFeedItem.Link + loopFeedItem.Title))
                        .ToString();

                if (string.IsNullOrWhiteSpace(correctedFeedId))
                {
                    returnErrors.Add(
                        $"Could Not Add an Item from {loopFeed.Name} - not enough information to generate in Id?");
                    continue;
                }

                if (db.FeedItems.Any(
                        x => x.FeedPersistentId == loopFeed.PersistentId && x.FeedItemId == correctedFeedId))
                {
                    existingItemCounter++;
                    continue;
                }

                newItemCounter++;

                var newFeedItem = new ReaderFeedItem
                {
                    CreatedOn = DateTime.Now,
                    FeedPersistentId = loopFeed.PersistentId,
                    Content = loopFeedItem.Content,
                    FeedItemId = correctedFeedId,
                    Title = loopFeedItem.Title,
                    Author = loopFeedItem.Author,
                    PublishingDate = loopFeedItem.PublishingDate ?? DateTime.Now,
                    Description = loopFeedItem.Description,
                    Link = loopFeedItem.Link,
                    PersistentId = Guid.NewGuid()
                };

                await db.FeedItems.AddAsync(newFeedItem);

                await db.SaveChangesAsync();

                newFeedItems.Add(newFeedItem.PersistentId);
            }

            totalNewItemsCounter += newItemCounter;
            totalExistingItemsCounter += existingItemCounter;

            await AutoMarkAfterDayRefreshFeed(loopFeed.PersistentId, progress);

            DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.FeedItem,
                DataNotificationUpdateType.New, newFeedItems);
        }

        return returnErrors;
    }

    public async Task<List<string>> UpdateFeeds(IProgress<string> progress)
    {
        var db = await GetInstance();

        var feedIds = await db.Feeds.OrderBy(x => x.Name).Select(x => x.PersistentId).ToListAsync();

        return await UpdateFeeds(feedIds, progress);
    }
}