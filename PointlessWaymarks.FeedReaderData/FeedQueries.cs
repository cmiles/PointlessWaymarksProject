using System.Security.Cryptography;
using System.Text;
using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData.Models;
using Serilog;
using Feed = PointlessWaymarks.FeedReaderData.Models.Feed;
using FeedItem = PointlessWaymarks.FeedReaderData.Models.FeedItem;

namespace PointlessWaymarks.FeedReaderData;

public static class FeedQueries
{
    public static async Task ArchiveFeed(Guid feedPersistentId, IProgress<string> progress)
    {
        var db = await FeedContext.CreateInstance();
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
                historicFeedItem = new HistoricFeedItem
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
            historicFeed = new HistoricFeed { PersistentId = toArchive.PersistentId };
            db.HistoricFeeds.Add(historicFeed);
        }

        historicFeed.InjectFrom(toArchive);

        db.Feeds.Remove(toArchive);

        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.Feed,
            DataNotificationUpdateType.Delete, historicFeed.PersistentId.AsList());
    }

    public static async Task FeedAllItemsRead(Guid feedId, bool markRead)
    {
        var db = await FeedContext.CreateInstance();

        var items = await db.FeedItems.Where(x => x.MarkedRead != markRead).OrderBy(x => x.FeedTitle)
            .Select(x => x.PersistentId).ToListAsync();

        await ItemRead(items, markRead);
    }

    public static async Task ItemKeepUnreadToggle(List<Guid> itemIds)
    {
        var db = await FeedContext.CreateInstance();

        var updateList = new List<Guid>();

        foreach (var loopIds in itemIds)
        {
            var item = db.FeedItems.SingleOrDefault(x => x.PersistentId == loopIds);

            if (item == null) return;

            item.KeepUnread = !item.KeepUnread;
            if (item.KeepUnread) item.MarkedRead = false;

            updateList.Add(item.PersistentId);
        }

        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.FeedItem,
            DataNotificationUpdateType.Update, updateList);
    }

    public static async Task ItemRead(List<Guid> itemIds, bool markRead)
    {
        var db = await FeedContext.CreateInstance();

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

    public static async Task<OneOf<Success, Error<string>>> TryAddFeed(string url, IProgress<string> progress)
    {
        if (string.IsNullOrEmpty(url)) return new Error<string>("Feed to Add Url is Blank?");

        var db = await FeedContext.CreateInstance();

        var cleanedUrl = url.Trim();

        if ((await db.Feeds.ToListAsync()).Any(x => x.Url.Equals(cleanedUrl, StringComparison.OrdinalIgnoreCase)))
            return new Error<string>("Feed already exists?");

        CodeHollow.FeedReader.Feed feedInfo;

        try
        {
            feedInfo = await FeedReader.ReadAsync(cleanedUrl);
        }
        catch (Exception e)
        {
            return new Error<string>($"Problem Adding Feed - {e.Message}");
        }

        var newFeed = new Feed
        {
            Name = feedInfo.Title,
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
    public static async Task<Feed> TryGetFeed(string url, IProgress<string> progress)
    {
        var toReturn = new Feed();

        if (string.IsNullOrEmpty(url)) return toReturn;

        var cleanedUrl = url.Trim();

        toReturn.Url = cleanedUrl;

        CodeHollow.FeedReader.Feed? feedInfo;

        try
        {
            feedInfo = await FeedReader.ReadAsync(cleanedUrl);
        }
        catch (Exception)
        {
            return toReturn;
        }

        if (feedInfo == null) return toReturn;

        toReturn.Name = feedInfo.Title;
        toReturn.FeedLastUpdatedDate = feedInfo.LastUpdatedDate;

        return toReturn;
    }

    public static async Task<List<string>> UpdateFeeds(List<Guid> toUpdate, IProgress<string> progress)
    {
        var db = await FeedContext.CreateInstance();

        var feeds = await db.Feeds.Where(x => toUpdate.Contains(x.PersistentId)).OrderBy(x => x.Name).ToListAsync();

        progress.Report($"Refreshing {feeds.Count} Feeds");

        var feedCounter = 0;
        var totalNewItemsCounter = 0;
        var totalExistingItemsCounter = 0;

        var newItems = new List<Guid>();
        var returnErrors = new List<string>();

        foreach (var loopFeed in feeds)
        {
            feedCounter++;

            progress.Report(
                $"Feed {loopFeed.Name} - {feedCounter} of {feeds.Count} - {totalNewItemsCounter} New, {totalExistingItemsCounter} Existing");

            CodeHollow.FeedReader.Feed? currentFeed;
            List<CodeHollow.FeedReader.FeedItem> currentFeedItems;

            try
            {
                currentFeed = await FeedReader.ReadAsync(loopFeed.Url);
            }
            catch (Exception e)
            {
                returnErrors.Add($"{loopFeed.Url} - {e.Message}");
                Log.ForContext(nameof(loopFeed), loopFeed.SafeObjectDump())
                    .Error(e, "Error Updating Feed {feedUrl}", loopFeed.Url);
                continue;
            }

            if (currentFeed == null)
            {
                returnErrors.Add($"{loopFeed.Url} - No Data");
                Log.ForContext(nameof(loopFeed), loopFeed.SafeObjectDump())
                    .Error("Null Return Updating Feed {feedUrl}", loopFeed.Url);
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

                if (db.FeedItems.Any(x => x.FeedPersistentId == loopFeed.PersistentId && x.FeedId == correctedFeedId))
                {
                    existingItemCounter++;
                    continue;
                }

                newItemCounter++;

                var newFeedItem = new FeedItem
                {
                    CreatedOn = DateTime.Now,
                    FeedPersistentId = loopFeed.PersistentId,
                    FeedContent = loopFeedItem.Content,
                    FeedId = correctedFeedId,
                    FeedTitle = loopFeedItem.Title,
                    FeedAuthor = loopFeedItem.Author,
                    FeedPublishingDate = loopFeedItem.PublishingDate ?? DateTime.Now,
                    FeedDescription = loopFeedItem.Description,
                    FeedLink = loopFeedItem.Link,
                    PersistentId = Guid.NewGuid()
                };

                await db.FeedItems.AddAsync(newFeedItem);

                await db.SaveChangesAsync();

                newItems.Add(newFeedItem.PersistentId);
            }

            totalNewItemsCounter += newItemCounter;
            totalExistingItemsCounter += existingItemCounter;
        }

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.FeedItem,
            DataNotificationUpdateType.New, newItems);

        return returnErrors;
    }

    public static async Task<List<string>> UpdateFeeds(IProgress<string> progress)
    {
        var db = await FeedContext.CreateInstance();

        var feedIds = await db.Feeds.OrderBy(x => x.Name).Select(x => x.PersistentId).ToListAsync();

        return await UpdateFeeds(feedIds, progress);
    }
}