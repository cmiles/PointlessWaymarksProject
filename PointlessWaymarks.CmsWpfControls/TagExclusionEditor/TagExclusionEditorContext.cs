using System.Collections.ObjectModel;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.TagExclusionEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class TagExclusionEditorContext
{
    private TagExclusionEditorContext(StatusControlContext statusContext,
        ObservableCollection<TagExclusionEditorListItem> itemCollection, bool loadInBackground = true)
    {
        StatusContext = statusContext;
        CommonCommands = new CmsCommonCommands(StatusContext);

        BuildCommands();

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        HelpMarkdown = TagExclusionHelpMarkdown.HelpBlock;

        Items = itemCollection;

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public string HelpMarkdown { get; set; }
    public ObservableCollection<TagExclusionEditorListItem> Items { get; set; }
    public StatusControlContext StatusContext { get; set; }

    [BlockingCommand]
    public async Task AddNewItem()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new TagExclusionEditorListItem { DbEntry = new TagExclusion() });
    }

    public static async Task<TagExclusionEditorContext> CreateInstance(StatusControlContext? statusContext,
        bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        return new TagExclusionEditorContext(statusContext ?? new StatusControlContext(),
            new ObservableCollection<TagExclusionEditorListItem>(), loadInBackground);
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs e)
    {
        var translatedMessage = DataNotifications.TranslateDataNotification(e.Message);

        if (translatedMessage.HasError)
        {
            Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                translatedMessage.ErrorNote, StatusContext.StatusControlContextId);
            return;
        }

        await LoadData();
    }

    [NonBlockingCommand]
    private async Task DeleteItem(TagExclusionEditorListItem? tagItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (tagItem == null)
        {
            StatusContext.ToastError("No Tag Item to Delete???");
            return;
        }

        if (tagItem.DbEntry == null || tagItem.DbEntry.Id < 1)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            Items.Remove(tagItem);
            return;
        }

        await Db.DeleteTagExclusion(tagItem.DbEntry.Id);

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Remove(tagItem);
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        var dbItems = await Db.TagExclusions();

        var listItems = dbItems.Select(x => new TagExclusionEditorListItem { DbEntry = x, TagValue = x.Tag })
            .OrderBy(x => x.TagValue).ToList();

        var currentItems = Items.ToList();

        foreach (var loopListItem in listItems)
        {
            var possibleItem = currentItems.Where(x => x.DbEntry?.Id != null)
                .SingleOrDefault(x => x.DbEntry?.Id == loopListItem.DbEntry?.Id);

            if (possibleItem == null)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Add(loopListItem);
                await ThreadSwitcher.ResumeBackgroundAsync();
            }
            else
            {
                possibleItem.DbEntry = loopListItem.DbEntry;
            }
        }

        var currentItemsWithDbEntry = currentItems.Where(x => x.DbEntry is { Id: >= 0 }).ToList();
        var newItemIds = listItems.Where(x => x.DbEntry?.Id != null).Select(x => x.DbEntry?.Id);

        var deletedItems = currentItemsWithDbEntry.Where(x => !newItemIds.Contains(x.DbEntry?.Id)).ToList();

        await ThreadSwitcher.ResumeForegroundAsync();
        foreach (var loopDeleted in deletedItems)
            try
            {
                Items.Remove(loopDeleted);
            }
            catch (Exception e)
            {
                Log.ForContext("loopDeleted", loopDeleted.SafeObjectDump()).Error(e,
                    "Suppressed error in Tag Exclusion Editor Context - delete item while Loading failed");
                Console.WriteLine(e);
            }
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }

    [NonBlockingCommand]
    private async Task SaveItem(TagExclusionEditorListItem? tagItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (tagItem == null)
        {
            StatusContext.ToastError("No Tag Item to Save???");
            return;
        }

        tagItem.TagValue = tagItem.TagValue.TrimNullToEmpty();

        var toSave = new TagExclusion();
        toSave.InjectFrom(tagItem.DbEntry);
        toSave.Tag = tagItem.TagValue.TrimNullToEmpty();

        var validation = await TagExclusionGenerator.Validate(toSave);

        if (validation.HasError)
        {
            StatusContext.ToastError($"Tag is not valid - {validation.GenerationNote}");
            return;
        }

        var saveReturn = await TagExclusionGenerator.Save(toSave);

        if (saveReturn.generationReturn.HasError)
        {
            StatusContext.ToastError($"Error Saving - {validation.GenerationNote}");
            return;
        }

        tagItem.DbEntry = saveReturn.returnContent;
    }
}