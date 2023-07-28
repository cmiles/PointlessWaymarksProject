using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.RssReaderData.Models;
using Serilog;

namespace PointlessWaymarks.RssReaderData;

public static class RssQueries
{
    public static async Task ItemKeepUnreadToggle(List<Guid> itemIds)
    {
        var db = await RssContext.CreateInstance();

        var updateList = new List<Guid>();

        foreach (var loopIds in itemIds)
        {
            var item = db.RssItems.SingleOrDefault(x => x.PersistentId == loopIds);

            if (item == null) return;

            item.KeepUnread = !item.KeepUnread;
            if (item.KeepUnread) item.MarkedRead = false;

            updateList.Add(item.PersistentId);
        }

        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.RssItem,
            DataNotificationUpdateType.Update, updateList);
    }

    public static async Task ItemRead(List<Guid> itemIds, bool markRead)
    {
        var db = await RssContext.CreateInstance();

        var updateList = new List<Guid>();

        foreach (var loopIds in itemIds)
        {
            var item = db.RssItems.SingleOrDefault(x => x.PersistentId == loopIds);

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

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.RssItem,
            DataNotificationUpdateType.Update, updateList);
    }

    public static async Task<OneOf<Success, Error<string>>> TryAddFeed(string url, IProgress<string> progress)
    {
        var returnErrors = new List<string>();

        if (string.IsNullOrEmpty(url)) return new Error<string>("Feed to Add Url is Blank?");

        var db = await RssContext.CreateInstance();

        var cleanedUrl = url.Trim();

        if ((await db.RssFeeds.ToListAsync()).Any(x => x.Url.Equals(cleanedUrl, StringComparison.OrdinalIgnoreCase)))
            return new Error<string>("Feed already exists?");

        Feed feedInfo;

        try
        {
            feedInfo = await FeedReader.ReadAsync(cleanedUrl);
        }
        catch (Exception e)
        {
            return new Error<string>($"Problem Adding Feed - {e.Message}");
        }

        var newFeed = new RssFeed
        {
            PersistentId = Guid.NewGuid(),
            Name = feedInfo.Title,
            Url = cleanedUrl
        };

        await db.RssFeeds.AddAsync(newFeed);

        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.RssFeed,
            DataNotificationUpdateType.New, newFeed.PersistentId.AsList());

        await UpdateFeeds(newFeed.PersistentId.AsList(), progress);

        return new Success();
    }


    public static async Task<List<string>> UpdateFeeds(List<Guid> toUpdate, IProgress<string> progress)
    {
        var db = await RssContext.CreateInstance();

        var feeds = await db.RssFeeds.Where(x => toUpdate.Contains(x.PersistentId)).OrderBy(x => x.Name).ToListAsync();

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

            Feed? currentFeed;
            List<FeedItem> currentFeedItems;

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

            progress.Report($"Feed {loopFeed.Name} - Found {currentFeedItems.Count} Feed Items to Process");

            var feedItemCounter = 0;
            var newItemCounter = 0;
            var existingItemCounter = 0;

            foreach (var loopFeedItem in currentFeedItems)
            {
                feedCounter++;

                if (feedCounter % 10 == 0)
                    progress.Report(
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
                    FeedLink = loopFeedItem.Link,
                    PersistentId = Guid.NewGuid()
                };

                await db.RssItems.AddAsync(newFeedItem);

                await db.SaveChangesAsync();

                newItems.Add(newFeedItem.PersistentId);
            }

            totalNewItemsCounter += newItemCounter;
            totalExistingItemsCounter += existingItemCounter;
        }

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.RssItem,
            DataNotificationUpdateType.New, newItems);

        return returnErrors;
    }

    public static async Task<List<string>> UpdateFeeds(IProgress<string> progress)
    {
        var db = await RssContext.CreateInstance();

        var feedIds = await db.RssFeeds.OrderBy(x => x.Name).Select(x => x.PersistentId).ToListAsync();

        return await UpdateFeeds(feedIds, progress);
    }
}