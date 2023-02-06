using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.TagExclusionEditor;

public partial class TagExclusionEditorContext : ObservableObject
{
    [ObservableProperty] private RelayCommand _addNewItemCommand;
    [ObservableProperty] private DataNotificationsWorkQueue _dataNotificationsProcessor;
    [ObservableProperty] private RelayCommand<TagExclusionEditorListItem> _deleteItemCommand;
    [ObservableProperty] private string _helpMarkdown;
    [ObservableProperty] private ObservableCollection<TagExclusionEditorListItem> _items;
    [ObservableProperty] private RelayCommand<TagExclusionEditorListItem> _saveItemCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private CmsCommonCommands _commonCommands;

    public TagExclusionEditorContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        CommonCommands = new CmsCommonCommands(StatusContext);

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        HelpMarkdown = TagExclusionHelpMarkdown.HelpBlock;
        AddNewItemCommand = StatusContext.RunBlockingTaskCommand(async () => await AddNewItem());
        SaveItemCommand = StatusContext.RunNonBlockingTaskCommand<TagExclusionEditorListItem>(SaveItem);
        DeleteItemCommand = StatusContext.RunNonBlockingTaskCommand<TagExclusionEditorListItem>(DeleteItem);

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public async Task AddNewItem()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(new TagExclusionEditorListItem { DbEntry = new TagExclusion() });
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

    private async Task DeleteItem(TagExclusionEditorListItem tagItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

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

        if (Items == null)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            Items = new ObservableCollection<TagExclusionEditorListItem>(listItems);
            await ThreadSwitcher.ResumeBackgroundAsync();
            return;
        }

        var currentItems = Items.ToList();

        foreach (var loopListItem in listItems)
        {
            var possibleItem = currentItems.SingleOrDefault(x => x.DbEntry?.Id == loopListItem.DbEntry.Id);

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
        var newItemIds = listItems.Select(x => x.DbEntry.Id);

        var deletedItems = currentItemsWithDbEntry.Where(x => !newItemIds.Contains(x.DbEntry.Id)).ToList();

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

    private void OnDataNotificationReceived(object sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }

    private async Task SaveItem(TagExclusionEditorListItem tagItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

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