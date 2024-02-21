using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CmsWpfControls.MapIconList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class MapIconListContext
{
    private MapIconListContext(ObservableCollection<MapIconListListItem> items, StatusControlContext statusContext)
    {
        Items = items;
        StatusContext = statusContext;

        BuildCommands();

        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };

        ListSort = new ColumnSortControlContext
        {
            Items =
            [
                new ColumnSortControlSortItem
                {
                    DisplayName = "Name",
                    ColumnName = "IconName",
                    DefaultSortDirection = ListSortDirection.Ascending,
                    Order = 1
                },

                new ColumnSortControlSortItem
                {
                    DisplayName = "Created",
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
    public ObservableCollection<MapIconListListItem> Items { get; set; }
    public ContentListSelected<MapIconListListItem> ListSelection { get; set; }
    public ColumnSortControlContext ListSort { get; set; }

    public StatusControlContext StatusContext { get; set; }

    public string UserFilterText { get; set; } = string.Empty;

    [BlockingCommand]
    public async Task AddNewListItem()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var toAdd = MapIconListListItem.CreateInstance(new MapIcon
        {
            ContentId = Guid.NewGuid(), LastUpdatedOn = DateTime.Now,
            LastUpdatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
        });

        Items.Add(toAdd);
    }

    public static async Task<MapIconListContext> CreateInstance(StatusControlContext? statusContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryItems = new ObservableCollection<MapIconListListItem>();

        return new MapIconListContext(factoryItems, factoryContext);
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
            translatedMessage.ContentType != DataNotificationContentType.MapIcon) return;

        var existingListItemsMatchingNotification = new List<MapIconListListItem>();

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
        var dbItems = await context.MapIcons.Where(x => translatedMessage.ContentIds.Contains(x.ContentId))
            .ToListAsync();

        foreach (var loopItem in dbItems)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var existingItems = existingListItemsMatchingNotification
                .Where(x => x.DbEntry.ContentId == loopItem.ContentId).ToList();

            if (existingItems.Count < 1)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Add(MapIconListListItem.CreateInstance(loopItem));
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

    public async Task DeleteItem(MapIconListListItem toSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = await Db.Context();

        var possibleExistingItem =
            await context.MapIcons.FirstOrDefaultAsync(x => x.ContentId == toSave.DbEntry.ContentId);

        if (possibleExistingItem != null)
        {
            var historicEntry = new HistoricMapIcon()
            {
                ContentId = possibleExistingItem.ContentId, IconName = possibleExistingItem.IconName,
                IconSource = possibleExistingItem.IconSource,
                IconSvg = possibleExistingItem.IconSvg, LastUpdatedBy = possibleExistingItem.LastUpdatedBy,
                LastUpdatedOn = possibleExistingItem.LastUpdatedOn
            };

            await context.HistoricMapIcons.AddAsync(historicEntry);
            context.Remove(possibleExistingItem);
        }

        DataNotifications.PublishDataNotification("Map Icon Deleted", DataNotificationContentType.MapIcon,
            DataNotificationUpdateType.Delete, toSave.DbEntry.ContentId.AsList());
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
            if (o is not MapIconListListItem listItem) return false;

            return listItem.IconName.ToUpper().Contains(UserFilterText.ToUpper().Trim());
        };
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = await Db.Context();

        var allDbIcons = await context.MapIcons.OrderBy(x => x.IconName).ToListAsync();

        var toLoad = allDbIcons.Select(MapIconListListItem.CreateInstance).ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        toLoad.ForEach(x => Items.Add(x));
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(UserFilterText))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    [BlockingCommand]
    public async Task SaveItem(MapIconListListItem toSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = await Db.Context();

        var possibleExistingItem =
            await context.MapIcons.FirstOrDefaultAsync(x => x.ContentId == toSave.DbEntry.ContentId);

        var toAdd = new MapIcon()
        {
            ContentId = toSave.DbEntry.ContentId, IconName = toSave.IconNameEntry.UserValue,
            IconSource = toSave.IconSourceEntry.UserValue,
            IconSvg = toSave.IconSvgEntry.UserValue, LastUpdatedBy = toSave.LastUpdatedByEntry.UserValue,
            LastUpdatedOn = DateTime.Now
        };

        if (possibleExistingItem != null && (possibleExistingItem.IconName != toAdd.IconName ||
                                             possibleExistingItem.IconSource != toAdd.IconSource ||
                                             possibleExistingItem.IconSvg != toAdd.IconSvg))
        {
            var historicEntry = new HistoricMapIcon()
            {
                ContentId = possibleExistingItem.ContentId, IconName = possibleExistingItem.IconName,
                IconSource = possibleExistingItem.IconSource,
                IconSvg = possibleExistingItem.IconSvg, LastUpdatedBy = possibleExistingItem.LastUpdatedBy,
                LastUpdatedOn = possibleExistingItem.LastUpdatedOn
            };

            await context.HistoricMapIcons.AddAsync(historicEntry);
            context.Remove(possibleExistingItem);
        }

        await context.AddAsync(toAdd);
        await context.SaveChangesAsync();

        if (possibleExistingItem is null)
            DataNotifications.PublishDataNotification("Map Icon Added", DataNotificationContentType.MapIcon,
                DataNotificationUpdateType.New, toAdd.ContentId.AsList());
        else
            DataNotifications.PublishDataNotification("Map Icon Updated", DataNotificationContentType.MapIcon,
                DataNotificationUpdateType.Update, toAdd.ContentId.AsList());
    }
}