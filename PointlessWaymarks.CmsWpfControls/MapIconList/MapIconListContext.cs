using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentGeneration;
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
    private MapIconListContext(ObservableCollection<MapIconListListItem> items, StatusControlContext statusContext,
        ContentListSelected<MapIconListListItem> factoryListSelection)
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
    private async Task AddDefaultLibraryIcons()
    {
        var defaultLibraryIcons = MapIconDefaultLibrary.DefaultIcons();

        var context = await Db.Context();

        var currentIcons = await context.MapIcons.Select(x => x.IconName).ToListAsync();

        var defaultIconsToAdd = defaultLibraryIcons.Where(x => !currentIcons.Contains(x.IconName)).ToList();

        var hasError = false;
        var errorList = new List<string>();

        foreach (var loopIcon in defaultIconsToAdd)
        {
            var result = await MapIconGenerator.SaveMapIconAndGenerateMapIconsJson(loopIcon);
            if (result.IsT1)
            {
                hasError = true;
                errorList.Add(result.AsT1.Value);
            }
        }

        if (hasError)
            await StatusContext.ShowMessageWithOkButton("Error Adding Default Icons",
                $"{string.Join(Environment.NewLine, errorList)}");
        else
            await StatusContext.ToastSuccess($"Added {defaultIconsToAdd.Count} Icons");
    }

    [BlockingCommand]
    public async Task AddNewListItem()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var toAdd = MapIconListListItem.CreateInstance(new MapIcon
        {
            ContentId = Guid.NewGuid(), LastUpdatedOn = DateTime.Now,
            LastUpdatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            ContentVersion = Db.ContentVersionDateTime()
        });

        Items.Add(toAdd);
    }

    public static async Task<MapIconListContext> CreateInstance(StatusControlContext? statusContext)
    {
        var factoryContext = await StatusControlContext.ResumeForegroundAsyncAndCreateInstance(statusContext);

        var factoryItems = new ObservableCollection<MapIconListListItem>();
        var factoryListSelection = await ContentListSelected<MapIconListListItem>.CreateInstance(factoryContext);

        return new MapIconListContext(factoryItems, factoryContext, factoryListSelection);
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

    [BlockingCommand]
    public async Task DeleteItem(MapIconListListItem toSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = await Db.Context();

        var possibleExistingItem =
            await context.MapIcons.FirstOrDefaultAsync(x => x.ContentId == toSave.DbEntry.ContentId);

        if (possibleExistingItem == null)
        {
            DataNotifications.PublishDataNotification("Map Icon Deleted", DataNotificationContentType.MapIcon,
                DataNotificationUpdateType.Delete, toSave.DbEntry.ContentId.AsList());
            return;
        }

        var uses = await context.PointContents
            .Where(x => x.MapIconName != null && x.MapIconName == toSave.DbEntry.IconName!.ToLower())
            .OrderBy(x => x.Title)
            .ToListAsync();

        if (uses.Any())
        {
            await StatusContext.ShowMessageWithOkButton("Map Icon Deletion Error",
                $"""
                 This Map Icon is still in use and can not be deleted - if you really want to delete this Map Icon delete or change the icon in the Points below:

                 {string.Join(Environment.NewLine, uses.Select(x => $"{x.Title} - [View](http://localactions.pointlesswaymarks.local/preview/{WebUtility.UrlEncode(x.ContentId.ToString())}) - [Edit](http://localactions.pointlesswaymarks.local/edit/{WebUtility.UrlEncode(x.ContentId.ToString())})"))}
                 """);
            return;
        }

        var historicEntry = new HistoricMapIcon
        {
            ContentId = possibleExistingItem.ContentId, IconName = possibleExistingItem.IconName,
            IconSource = possibleExistingItem.IconSource,
            IconSvg = possibleExistingItem.IconSvg, LastUpdatedBy = possibleExistingItem.LastUpdatedBy,
            LastUpdatedOn = possibleExistingItem.LastUpdatedOn,
            ContentVersion = possibleExistingItem.ContentVersion
        };

        await context.HistoricMapIcons.AddAsync(historicEntry);
        context.Remove(possibleExistingItem);

        await context.SaveChangesAsync();

        DataNotifications.PublishDataNotification("Map Icon Deleted", DataNotificationContentType.MapIcon,
            DataNotificationUpdateType.Delete, historicEntry.ContentId.AsList());

        await MapIconGenerator.GenerateMapIconsFile();

        await StatusContext.ToastSuccess($"Deleted {historicEntry.IconName}");
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

        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        var context = await Db.Context();

        var allDbIcons = await context.MapIcons.OrderBy(x => x.IconName).ToListAsync();

        var toLoad = allDbIcons.Select(MapIconListListItem.CreateInstance).ToList();

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

    [BlockingCommand]
    public async Task SaveItem(MapIconListListItem toSave)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!toSave.HasChanges)
        {
            await StatusContext.ToastWarning("No Changes to Save?");
            return;
        }

        if (toSave.HasValidationIssues)
        {
            await StatusContext.ToastError("Can't Save - Validation Issues");
            return;
        }

        var toAdd = new MapIcon
        {
            ContentId = toSave.DbEntry.ContentId, IconName = toSave.IconNameEntry.UserValue,
            IconSource = toSave.IconSourceEntry.UserValue,
            IconSvg = toSave.IconSvgEntry.UserValue, LastUpdatedBy = toSave.LastUpdatedByEntry.UserValue,
            LastUpdatedOn = DateTime.Now,
            ContentVersion = toSave.DbEntry.ContentVersion
        };

        var saveResult = await MapIconGenerator.SaveMapIconAndGenerateMapIconsJson(toAdd);

        if (saveResult.IsT1)
            await StatusContext.ShowMessageWithOkButton($"Error Saving {toAdd.IconName}", saveResult.AsT1.Value);
        else
            await StatusContext.ToastSuccess($"Saved {toAdd.IconName}");
    }
}