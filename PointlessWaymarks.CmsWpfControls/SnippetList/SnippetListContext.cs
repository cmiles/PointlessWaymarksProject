using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using GongSolutions.Wpf.DragDrop;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.SnippetEditor;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.SnippetList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class SnippetListContext : IDragSource, IDropTarget
{
    private SnippetListContext(ObservableCollection<SnippetListListItem> items, StatusControlContext statusContext,
        ContentListSelected<SnippetListListItem> factoryListSelection, bool loadInBackground = true)
    {
        Items = items;
        StatusContext = statusContext;
        CommonCommands = new CmsCommonCommands(StatusContext);
        ListSelection = factoryListSelection;

        BuildCommands();

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        ListSort = new ColumnSortControlContext
        {
            Items =
            [
                new ColumnSortControlSortItem
                {
                    DisplayName = "Title",
                    ColumnName = "Title",
                    DefaultSortDirection = ListSortDirection.Ascending,
                    Order = 1
                },

                new ColumnSortControlSortItem
                {
                    DisplayName = "Updated On",
                    ColumnName = "DbEntry.LastUpdatedOn",
                    DefaultSortDirection = ListSortDirection.Descending
                }
            ]
        };

        ListSort.SortUpdated += (_, list) =>
            StatusContext.RunFireAndForgetNonBlockingTask(() => ListContextSortHelpers.SortList(list, Items));

        PropertyChanged += OnPropertyChanged;

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public ObservableCollection<SnippetListListItem> Items { get; set; }
    public ContentListSelected<SnippetListListItem> ListSelection { get; set; }
    public ColumnSortControlContext ListSort { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string UserFilterText { get; set; } = string.Empty;

    public bool CanStartDrag(IDragInfo dragInfo)
    {
        return ListSelection.SelectedItems.Count > 0;
    }

    public void DragCancelled()
    {
    }

    public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
    {
    }

    public void Dropped(IDropInfo dropInfo)
    {
    }

    public void StartDrag(IDragInfo dragInfo)
    {
        var defaultBracketCodeList = ListSelection.SelectedItems.Select(x => DefaultBracketCode(x.DbEntry)).ToList();
        dragInfo.Data = string.Join(Environment.NewLine, defaultBracketCodeList);
        dragInfo.DataFormat = DataFormats.GetDataFormat(DataFormats.UnicodeText);
        dragInfo.Effects = DragDropEffects.Copy;
    }

    public bool TryCatchOccurredException(Exception exception)
    {
        return false;
    }

    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is string ||
            (dropInfo.Data is DataObject dataObject && dataObject.GetDataPresent(DataFormats.UnicodeText)))
            dropInfo.Effects = DragDropEffects.Copy;
        else
            dropInfo.Effects = DragDropEffects.None;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is string droppedString)
        {
            StatusContext.RunNonBlockingTask(async () => await HandleDroppedString(droppedString));
        }
        else if (dropInfo.Data is DataObject dataObject && dataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            if (dataObject.GetData(DataFormats.UnicodeText) is string droppedDataString)
                StatusContext.RunNonBlockingTask(async () => await HandleDroppedString(droppedDataString));
            else
                StatusContext.RunNonBlockingTask(async () =>
                    await StatusContext.ToastError("Failed to convert data to string."));
        }
        else
        {
            StatusContext.RunNonBlockingTask(
                async () => await StatusContext.ToastError("Only string data is accepted."));
        }
    }

    [NonBlockingCommand]
    public async Task BracketCodeToClipboardSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var selected = ListSelection.SelectedItems;

        if (!selected.Any())
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = string.Join(Environment.NewLine, selected.Select(x => BracketCodeSnippet.Create(x.DbEntry)));

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        await StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public static async Task<SnippetListContext> CreateInstance(StatusControlContext? statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        var factoryItems = new ObservableCollection<SnippetListListItem>();
        var factoryListSelection = await ContentListSelected<SnippetListListItem>.CreateInstance(factoryStatusContext);

        return new SnippetListContext(factoryItems, factoryStatusContext, factoryListSelection);
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

        if (!translatedMessage.ContentIds.Any() ||
            translatedMessage.ContentType != DataNotificationContentType.Snippet) return;

        var existingListItemsMatchingNotification = new List<SnippetListListItem>();

        foreach (var loopItem in Items)
        {
            var id = loopItem.DbEntry.ContentId;
            if (translatedMessage.ContentIds.Contains(id))
                existingListItemsMatchingNotification.Add(loopItem);
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (translatedMessage.UpdateType == DataNotificationUpdateType.Delete)
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            existingListItemsMatchingNotification.ForEach(x => Items.Remove(x));
            return;
        }

        var context = await Db.Context();
        var dbItems = await context.Snippets.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
            .ToListAsync();

        foreach (var loopItem in dbItems)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var existingItems = existingListItemsMatchingNotification
                .Where(x => x.DbEntry.ContentId == loopItem.ContentId).ToList();

            if (existingItems.Count < 1)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Add(SnippetListListItem.CreateInstance(loopItem));
                continue;
            }

            if (existingItems.Count > 1)
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                foreach (var loopDelete in existingItems.Skip(1).ToList()) Items.Remove(loopDelete);
            }

            var existingItem = existingItems.First();

            existingItem.DbEntry = loopItem;
        }

        await ListContextSortHelpers.SortList(ListSort.SortDescriptions(), Items);
        await FilterList();
    }

    public string DefaultBracketCode(Snippet? content)
    {
        if (content?.ContentId == null) return string.Empty;
        return $"{BracketCodeSnippet.Create(content)}";
    }

    [BlockingCommand]
    public async Task DefaultBracketCodeToClipboard(Snippet? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = $"{BracketCodeSnippet.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        await StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [NonBlockingCommand]
    public async Task Delete(Snippet? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            await StatusContext.ToastError($"Snippet {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        await Db.DeleteSnippet(content.ContentId, StatusContext.ProgressTracker());
    }

    [NonBlockingCommand]
    public async Task Edit(Snippet? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.Snippets.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
        {
            await StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");
            return;
        }

        var newContentWindow = await SnippetEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [NonBlockingCommand]
    public async Task EditSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var selected = ListSelection.Selected;

        if (selected == null)
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();

        var refreshedData = context.Snippets.SingleOrDefault(x => x.ContentId == selected.DbEntry.ContentId);

        if (refreshedData == null)
        {
            await StatusContext.ToastError(
                $"{selected.DbEntry.Title} is no longer active in the database? Can not edit - look for a historic version...");
            return;
        }

        var newContentWindow = await SnippetEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
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

        ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = o =>
        {
            if (o is not SnippetListListItem listItem) return false;

            return listItem.DbEntry.Title?.ToUpper().Contains(UserFilterText.ToUpper().Trim()) ?? false;
        };
    }

    public async Task HandleDroppedString(string? stringContent)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newSnippet = Snippet.CreateInstance();
        newSnippet.BodyContent = stringContent ?? string.Empty;

        await SnippetEditorWindow.CreateInstance(newSnippet, true);
    }

    [BlockingCommand]
    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        var context = await Db.Context();

        var allDbItems = await context.Snippets.OrderBy(x => x.Title).ToListAsync();

        var toLoad = allDbItems.Select(SnippetListListItem.CreateInstance).ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        toLoad.ForEach(x => Items.Add(x));

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    [NonBlockingCommand]
    public async Task NewSnippet()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContentWindow = await SnippetEditorWindow.CreateInstance();

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor.Enqueue(e);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(UserFilterText))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    [NonBlockingCommand]
    public async Task ViewHistory(Snippet? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricSnippets.Where(x => x.ContentId == content.ContentId).ToListAsync();

        StatusContext.Progress($"Found {historicItems.Count} Historic Entries");

        if (historicItems.Count < 1)
        {
            await StatusContext.ToastWarning("No History to Show...");
            return;
        }

        var historicView = new ContentViewHistoryPage($"Historic Entries - {content.Title}",
            UserSettingsSingleton.CurrentSettings().SiteName, $"Historic Entries - {content.Title}",
            historicItems.OrderByDescending(x => x.LastUpdatedOn.HasValue).ThenByDescending(x => x.LastUpdatedOn)
                .Select(LogTools.SafeObjectDump).ToList());

        historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
    }

    [NonBlockingCommand]
    public async Task ViewHistorySelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var selected = ListSelection.Selected;

        if (selected == null)
        {
            await StatusContext.ToastError("Nothing Selected?");
            return;
        }

        await ViewHistory(selected.DbEntry);
    }
}