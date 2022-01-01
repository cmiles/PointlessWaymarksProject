using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.GeoJsonContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;

using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonList;

public partial class GeoJsonContentActions : ObservableObject, IContentActions<GeoJsonContent>
{
    [ObservableProperty] private RelayCommand<GeoJsonContent> _deleteCommand;
    [ObservableProperty] private RelayCommand<GeoJsonContent> _editCommand;
    [ObservableProperty] private RelayCommand<GeoJsonContent> _extractNewLinksCommand;
    [ObservableProperty] private RelayCommand<GeoJsonContent> _generateHtmlCommand;
    [ObservableProperty] private RelayCommand<GeoJsonContent> _linkCodeToClipboardCommand;
    [ObservableProperty] private RelayCommand<GeoJsonContent> _viewOnSiteCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand<GeoJsonContent> _viewHistoryCommand;

    public GeoJsonContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        DeleteCommand = StatusContext.RunBlockingTaskCommand<GeoJsonContent>(Delete);
        EditCommand = StatusContext.RunNonBlockingTaskCommand<GeoJsonContent>(Edit);
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<GeoJsonContent>(ExtractNewLinks);
        GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<GeoJsonContent>(GenerateHtml);
        LinkCodeToClipboardCommand =
            StatusContext.RunBlockingTaskCommand<GeoJsonContent>(DefaultBracketCodeToClipboard);
        ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand<GeoJsonContent>(ViewOnSite);
        ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<GeoJsonContent>(ViewHistory);
    }

    public string DefaultBracketCode(GeoJsonContent content)
    {
        return content?.ContentId == null ? string.Empty : @$"{BracketCodeGeoJson.Create(content)}";
    }

    public async Task DefaultBracketCodeToClipboard(GeoJsonContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = @$"{BracketCodeGeoJson.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public async Task Delete(GeoJsonContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError($"GeoJson {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        await Db.DeleteGeoJsonContent(content.ContentId, StatusContext.ProgressTracker());

        var possibleContentDirectory = settings.LocalSiteGeoJsonContentDirectory(content, false);
        if (possibleContentDirectory.Exists)
        {
            StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
            possibleContentDirectory.Delete(true);
        }
    }

    public async Task Edit(GeoJsonContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.GeoJsonContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = new GeoJsonContentEditorWindow(refreshedData);

        newContentWindow.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();
    }

    public async Task ExtractNewLinks(GeoJsonContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();

        var refreshedData = context.GeoJsonContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
    }

    public async Task GenerateHtml(GeoJsonContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        var htmlContext = new SingleGeoJsonPage(content);

        await htmlContext.WriteLocalHtml();

        StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
    }

    public async Task ViewOnSite(GeoJsonContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"https:{settings.GeoJsonPageUrl(content)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    public async Task ViewHistory(GeoJsonContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricGeoJsonContents.Where(x => x.ContentId == content.ContentId).ToListAsync();

        StatusContext.Progress($"Found {historicItems.Count} Historic Entries");

        if (historicItems.Count < 1)
        {
            StatusContext.ToastWarning("No History to Show...");
            return;
        }

        var historicView = new ContentViewHistoryPage($"Historic Entries - {content.Title}",
            UserSettingsSingleton.CurrentSettings().SiteName, $"Historic Entries - {content.Title}",
            historicItems.OrderByDescending(x => x.LastUpdatedOn.HasValue).ThenByDescending(x => x.LastUpdatedOn)
                .Select(LogHelpers.SafeObjectDump).ToList());

        historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
    }

    public static GeoJsonListListItem ListItemFromDbItem(GeoJsonContent content, GeoJsonContentActions itemActions,
        bool showType)
    {
        return new()
        {
            DbEntry = content,
            SmallImageUrl = ContentListContext.GetSmallImageUrl(content),
            ItemActions = itemActions,
            ShowType = showType
        };
    }
}