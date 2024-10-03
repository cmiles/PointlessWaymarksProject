using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
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
public partial class SnippetListContext
{
    private SnippetListContext(ObservableCollection<SnippetListListItem> items, StatusControlContext statusContext,
        ContentListSelected<SnippetListListItem> factoryListSelection)
    {
        Items = items;
        StatusContext = statusContext;
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
    }


    public DataNotificationsWorkQueue DataNotificationsProcessor { get; set; }
    public ObservableCollection<SnippetListListItem> Items { get; set; }
    public ContentListSelected<SnippetListListItem> ListSelection { get; set; }
    public ColumnSortControlContext ListSort { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string UserFilterText { get; set; } = string.Empty;

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

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        var context = await Db.Context();

        var allDbIcons = await context.Snippets.OrderBy(x => x.Title).ToListAsync();

        var toLoad = allDbIcons.Select(SnippetListListItem.CreateInstance).ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        toLoad.ForEach(x => Items.Add(x));

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
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
}