using System.Windows;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.MapComponentData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.MapComponentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList;

public partial class MapComponentContentActions : ObservableObject, IContentActions<MapComponent>
{
    [ObservableProperty] private RelayCommand<MapComponent> _deleteCommand;
    [ObservableProperty] private RelayCommand<MapComponent> _editCommand;
    [ObservableProperty] private RelayCommand<MapComponent> _extractNewLinksCommand;
    [ObservableProperty] private RelayCommand<MapComponent> _generateHtmlCommand;
    [ObservableProperty] private RelayCommand<MapComponent> _linkCodeToClipboardCommand;
    [ObservableProperty] private RelayCommand<MapComponent> _viewOnSiteCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand<MapComponent> _viewHistoryCommand;

    public MapComponentContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        DeleteCommand = StatusContext.RunBlockingTaskCommand<MapComponent>(Delete);
        EditCommand = StatusContext.RunNonBlockingTaskCommand<MapComponent>(Edit);
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<MapComponent>(ExtractNewLinks);
        GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<MapComponent>(GenerateHtml);
        LinkCodeToClipboardCommand = StatusContext.RunBlockingTaskCommand<MapComponent>(DefaultBracketCodeToClipboard);
        ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand<MapComponent>(ViewOnSite);
        ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<MapComponent>(ViewHistory);
    }

    public string DefaultBracketCode(MapComponent content)
    {
        return content?.ContentId == null ? string.Empty : @$"{BracketCodeMapComponents.Create(content)}";
    }

    public async Task DefaultBracketCodeToClipboard(MapComponent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = @$"{BracketCodeMapComponents.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public async Task Delete(MapComponent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError($"Map {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        await Db.DeleteMapComponent(content.ContentId, StatusContext.ProgressTracker());
    }

    public async Task Edit(MapComponent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.MapComponents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        var newContentWindow = await MapComponentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public async Task ExtractNewLinks(MapComponent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();

        var refreshedData = context.MapComponents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors($"{refreshedData.UpdateNotes}",
            StatusContext.ProgressTracker());
    }

    public async Task GenerateHtml(MapComponent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        await MapData.WriteJsonData(content.ContentId);

        StatusContext.ToastSuccess("Generated Map Data");
    }

    public async Task ViewOnSite(MapComponent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.ToastWarning("Maps don't have a direct URL to open...");
    }

    public async Task ViewHistory(MapComponent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricMapComponents.Where(x => x.ContentId == content.ContentId).ToListAsync();

        StatusContext.Progress($"Found {historicItems.Count} Historic Entries");

        if (historicItems.Count < 1)
        {
            StatusContext.ToastWarning("No History to Show...");
            return;
        }

        var historicView = new ContentViewHistoryPage($"Historic Entries - {content.Title}",
            UserSettingsSingleton.CurrentSettings().SiteName, $"Historic Entries - {content.Title}",
            historicItems.OrderByDescending(x => x.LastUpdatedOn.HasValue).ThenByDescending(x => x.LastUpdatedOn)
                .Select(PointlessWaymarksLogTools.SafeObjectDump).ToList());

        historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
    }

    public static MapComponentListListItem ListItemFromDbItem(MapComponent content,
        MapComponentContentActions itemActions, bool showType)
    {
        return new() { DbEntry = content, ItemActions = itemActions, ShowType = showType };
    }
}