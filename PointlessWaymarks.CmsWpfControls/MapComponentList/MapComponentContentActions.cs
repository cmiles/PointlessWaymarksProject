using System.ComponentModel;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.MapComponentData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.MapComponentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class MapComponentContentActions : IContentActions<MapComponent>
{
    public MapComponentContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        BuildCommands();
    }

    public StatusControlContext StatusContext { get; set; }

    public string DefaultBracketCode(MapComponent? content)
    {
        return content?.ContentId == null ? string.Empty : $"{BracketCodeMapComponents.Create(content)}";
    }

    [BlockingCommand]
    public async Task DefaultBracketCodeToClipboard(MapComponent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = $"{BracketCodeMapComponents.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    public async Task Delete(MapComponent? content)
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

    [NonBlockingCommand]
    public async Task Edit(MapComponent? content)
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

    [BlockingCommand]
    public async Task ExtractNewLinks(MapComponent? content)
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

    [BlockingCommand]
    public async Task GenerateHtml(MapComponent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        var dto = await Db.MapComponentDtoFromContentId(content.ContentId);

        await Export.WriteMapComponentContentData(dto, StatusContext.ProgressTracker());

        StatusContext.ToastSuccess("Generated Map Data");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NonBlockingCommand]
    public async Task ViewHistory(MapComponent? content)
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
                .Select(LogTools.SafeObjectDump).ToList());

        historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task ViewOnSite(MapComponent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.ToastWarning("Maps don't have a direct URL to open...");
    }

    [BlockingCommand]
    public async Task ViewSitePreview(MapComponent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.ToastWarning("Maps don't have a direct URL to open...");
    }

    public static async Task<MapComponentListListItem> ListItemFromDbItem(MapComponent content,
        MapComponentContentActions itemActions, bool showType)
    {
        var item = await MapComponentListListItem.CreateInstance(itemActions);
        item.DbEntry = content;
        item.ShowType = showType;
        return item;
    }
}