using System.Text;
using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.RssReaderData;
using PointlessWaymarks.RssReaderData.Models;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.MarkdownDisplay;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.RssReaderGui.Controls;

[GenerateStatusCommands]
[NotifyPropertyChanged]
public partial class FeedEditorContext : IHasChanges, IHasValidationIssues,
    ICheckForChangesAndValidation
{
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required RssFeed DbFeedItem { get; set; }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    public HelpDisplayContext? HelpContext { get; set; }

    public string HelpText => 
"""
## Feed Editor

The Feed Editor allows you to create and edit Feeds, show the current results of parsing the Feed URL and displays some basic information about the feed.

  - Name: When creating a new Feed the program will try to extract a value for Name from the Feed - but this value can be anything you want to help you identify the Feed.
  - Notes (Optional): Years and years later it can sometimes be hard to remember why you added a feed, what is was or why it was important to you... Notes is available to use for anything you want!
  - Tags (Optional): Used for Filtering the Feed Items list this is intended to be a comma separated list, but could be a single word or nothing.
  - URL: This is the Feed's URL - remember that the Feed URL is not the same as a website's address - the URL for Pointless Waymarks is https://pointlesswaymarks.com/ - the feed URL is https://PointlessWaymarks.com/RssIndexFeed.xml - sites sometimes show a link on one of the top level pages for a Feed, but there are also browser extensions that can help you find Feed URLs.

URL Parse Result: When you update the URL (or use the Refresh Button) the program will show a simple text view of parsing the Feed. This can be useful for making sure you have the right URL and sometimes the correct feed - some sites will offer multiple feeds, for example one for posts and one for comments. The program will NOT stop you from saving a broken or invalid Feed URL - no Feed/Website is up and running without errors 100% of the time - the URL Parse Result is the result of parsing the feed 'now', useful but it has no ability to show you 'was it working yesterday and will it work again in the future'.
""";

    public EventHandler? RequestContentEditorWindowClose { get; set; }
    public required StatusControlContext StatusContext { get; init; }
    public bool UrlCheckHasError { get; set; }
    public bool UrlCheckHasWarning { get; set; }
    public string UrlCheckMessage { get; set; } = string.Empty;
    public required StringDataEntryContext UserNameEntry { get; init; }
    public required StringDataEntryContext UserNoteEntry { get; init; }
    public required StringDataEntryContext UserTagsEntry { get; init; }
    public required StringDataEntryContext UserUrlEntry { get; init; }

    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues =
            PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }

    private async Task<OneOf<Success<string>, Warning<string>, Error<string>>> CheckFeedUrl(string url)
    {
        try
        {
            var results = await FeedReader.ReadAsync(url);

            if (results is null) return new Error<string>("Null Result?");

            if (!results.Items.Any()) return new Warning<string>("No Feed Items?");

            var db = await RssContext.CreateInstance();

            if (db.RssFeeds.Any(x => x.Url == url && x.PersistentId != DbFeedItem.PersistentId)) return new Error<string>("Feed Already Exists?");

            var resultBuilder = new StringBuilder();

            resultBuilder.AppendLine(
                $"{results.Items.Count} Items, Title: {results.Title}, Last Update: {results.LastUpdatedDateString}");
            resultBuilder.AppendLine($"Description: {results.Description}");
            resultBuilder.AppendLine($"Copyright: {results.Copyright}");
            resultBuilder.AppendLine($"FeedType: {results.Type.ToString()}");
            resultBuilder.AppendLine($"Items ({results.Items.Count}):");

            foreach (var loopItem in results.Items)
                resultBuilder.AppendLine($"  {loopItem.PublishingDateString} - {loopItem.Title} - {loopItem.Author}");

            return new Success<string>(resultBuilder.ToString());
        }
        catch (Exception e)
        {
            return new Error<string>($"{url} - {e.Message}");
        }
    }

    public static async Task<FeedEditorContext> CreateInstance(StatusControlContext context, RssFeed? feedItem)
    {
        feedItem ??= new RssFeed();

        var userNameEntry = StringDataEntryContext.CreateInstance();
        userNameEntry.Title = "Name";
        userNameEntry.HelpText =
            "Your name for the feed - the program might be able to determine a value but change it as you like.";
        userNameEntry.ReferenceValue = feedItem.Id > 0 ? feedItem.Name : string.Empty;
        userNameEntry.UserValue = feedItem.Name;
        userNameEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            x =>
            {
                if (string.IsNullOrWhiteSpace(x))
                    return Task.FromResult(new IsValid(false, "A Name is required"));
                return Task.FromResult(new IsValid(true, string.Empty));
            }
        };

        var userNoteEntry = StringDataEntryContext.CreateInstance();
        userNoteEntry.Title = "Notes";
        userNoteEntry.HelpText =
            "Use for any notes that you want about the feed - this might not be useful for all feeds but over time notes might help you remember what a feed is and why you subscribed!";
        userNoteEntry.ReferenceValue = feedItem.Id > 0 ? feedItem.Note : string.Empty;
        userNoteEntry.UserValue = feedItem.Note;
        userNoteEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            _ => Task.FromResult(new IsValid(true, string.Empty))
        };

        var userUrlEntry = StringDataEntryContext.CreateInstance();
        userUrlEntry.Title = "URL";
        userUrlEntry.HelpText =
            "A URL for an RSS or Atom Feed.";
        userUrlEntry.ReferenceValue = feedItem.Id > 0 ? feedItem.Url : string.Empty;
        userUrlEntry.UserValue = feedItem.Url;
        userUrlEntry.BindingDelay = 800;

        var userTagEntry = StringDataEntryContext.CreateInstance();
        userTagEntry.Title = "Tags";
        userTagEntry.HelpText =
            "A comma separated list of tags - this can be used for filtering the feeds.";
        userTagEntry.ReferenceValue = feedItem.Id > 0 ? feedItem.Tags : string.Empty;
        userTagEntry.UserValue = feedItem.Tags;
        userTagEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            _ => Task.FromResult(new IsValid(true, string.Empty))
        };

        var newContext = new FeedEditorContext
        {
            DbFeedItem = feedItem,
            StatusContext = context,
            UserNameEntry = userNameEntry,
            UserNoteEntry = userNoteEntry,
            UserUrlEntry = userUrlEntry,
            UserTagsEntry = userTagEntry
        };

        newContext.UserUrlEntry.ValidationFunctions = new List<Func<string?, Task<IsValid>>>
        {
            async x => await newContext.ProcessCheckFeedUrl(x ?? string.Empty)
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

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
    }

    [NonBlockingCommand]
    public async Task UpdateUrlCheck()
    {
        await UserUrlEntry.CheckForChangesAndValidationIssues();
    }

    public async Task<IsValid> ProcessCheckFeedUrl(string url)
    {
        var result = await CheckFeedUrl(url);
        IsValid toReturn = new IsValid(false, "An Error Occurred");

        result.Switch(success =>
        {
            UrlCheckHasError = false;
            UrlCheckHasWarning = false;
            UrlCheckMessage = success.Value;
            toReturn = new IsValid(true, UrlCheckMessage);
        }, warning =>
        {
            UrlCheckHasError = false;
            UrlCheckHasWarning = true;
            UrlCheckMessage = warning.Value;
            toReturn = new IsValid(true, UrlCheckMessage);
        }, error =>
        {
            UrlCheckHasError = true;
            UrlCheckHasWarning = false;
            UrlCheckMessage = error.Value;
            toReturn = new IsValid(false, UrlCheckMessage);
        });

        return toReturn;
    }

    public int DbReadRssItems { get; set; }
    public int DbUnReadRssItems { get; set; }
    public int DbKeptUnReadRssItems { get; set; }

    public async Task UpdateDbReadStats()
    {
        var db = await RssContext.CreateInstance();

        DbReadRssItems =
            await db.RssItems.CountAsync(x => x.RssFeedPersistentId == DbFeedItem.PersistentId && x.MarkedRead);
        DbUnReadRssItems =
            await db.RssItems.CountAsync(x => x.RssFeedPersistentId == DbFeedItem.PersistentId && !x.MarkedRead);
        DbReadRssItems =
            await db.RssItems.CountAsync(x => x.RssFeedPersistentId == DbFeedItem.PersistentId && x.KeepUnread);
    }

    private async Task ProcessDataUpdateNotification(InterProcessDataNotification interProcessUpdateNotification)
    {
        if (interProcessUpdateNotification.ContentType == DataNotificationContentType.RssFeed &&
            interProcessUpdateNotification.ContentIds.Contains(DbFeedItem.PersistentId))
            if (interProcessUpdateNotification.UpdateType == DataNotificationUpdateType.Update ||
                interProcessUpdateNotification.UpdateType == DataNotificationUpdateType.New)
            {
                var db = await RssContext.CreateInstance();
                var dbFeedItem = await db.RssFeeds.SingleAsync(x => x.PersistentId == DbFeedItem.PersistentId);
                UserUrlEntry.ReferenceValue = dbFeedItem.Url;
                UserNameEntry.ReferenceValue = dbFeedItem.Name;
                UserNoteEntry.ReferenceValue = dbFeedItem.Note;
                UserTagsEntry.ReferenceValue = dbFeedItem.Tags;
                DbFeedItem = dbFeedItem;
            }

        if (interProcessUpdateNotification.ContentType == DataNotificationContentType.RssItem &&
            interProcessUpdateNotification.ContentIds.Contains(DbFeedItem.PersistentId))
        {
            await UpdateDbReadStats();
        }
    }

    [BlockingCommand]
    public async Task Save()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (HasValidationIssues)
        {
            StatusContext.ToastError("Please fix validation issues before saving");
            return;
        }

        if (UrlCheckHasWarning)
        {
            var continueAfterWarning =
                await StatusContext.ShowMessageWithYesNoButton("Warning", "URL has a Warning - Save Anyway?");
            if (continueAfterWarning.Equals("No", StringComparison.OrdinalIgnoreCase)) return;
        }

        if (UrlCheckHasError)
        {
            var continueAfterWarning =
                await StatusContext.ShowMessageWithYesNoButton("Warning", "URL has an Error - Save Anyway?");
            if (continueAfterWarning.Equals("No", StringComparison.OrdinalIgnoreCase)) return;
        }

        var db = await RssContext.CreateInstance();

        DbFeedItem.Name = UserNameEntry.UserValue;
        DbFeedItem.Note = UserNoteEntry.UserValue;
        DbFeedItem.Tags = UserTagsEntry.UserValue;
        DbFeedItem.Url = UserUrlEntry.UserValue;

        if (DbFeedItem.Id == 0)
        {
            db.RssFeeds.Add(DbFeedItem);
            await db.SaveChangesAsync();

            DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.RssFeed,
                DataNotificationUpdateType.Update, DbFeedItem.PersistentId.AsList());
        }
        else
        {
            db.RssFeeds.Update(DbFeedItem);
        }

        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification(LogTools.GetCaller(), DataNotificationContentType.RssFeed,
            DataNotificationUpdateType.Update, DbFeedItem.PersistentId.AsList());
    }

    [BlockingCommand]
    public async Task SaveAndClose()
    {
        await Save();

        RequestContentEditorWindowClose?.Invoke(this, EventArgs.Empty);
    }

    public async Task Setup()
    {
        BuildCommands();

        HelpContext = new HelpDisplayContext(new List<string>
        {
            HelpText
        });

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this,
            CheckForChangesAndValidationIssues);

        CheckForChangesAndValidationIssues();

        await UpdateDbReadStats();
    }
}