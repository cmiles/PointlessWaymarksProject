using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PointHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PointList;

public partial class PointContentActions : ObservableObject, IContentActions<PointContent>
{
    [ObservableProperty] private RelayCommand<PointContent> _deleteCommand;
    [ObservableProperty] private RelayCommand<PointContent> _editCommand;
    [ObservableProperty] private RelayCommand<PointContent> _extractNewLinksCommand;
    [ObservableProperty] private RelayCommand<PointContent> _generateHtmlCommand;
    [ObservableProperty] private RelayCommand<PointContent> _linkCodeToClipboardCommand;
    [ObservableProperty] private RelayCommand<PointContent> _viewOnSiteCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand<PointContent> _viewHistoryCommand;

    public PointContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        DeleteCommand = StatusContext.RunBlockingTaskCommand<PointContent>(Delete);
        EditCommand = StatusContext.RunNonBlockingTaskCommand<PointContent>(Edit);
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<PointContent>(ExtractNewLinks);
        GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<PointContent>(GenerateHtml);
        LinkCodeToClipboardCommand = StatusContext.RunBlockingTaskCommand<PointContent>(DefaultBracketCodeToClipboard);
        ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand<PointContent>(ViewOnSite);
        ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<PointContent>(ViewHistory);
    }

    public string DefaultBracketCode(PointContent content)
    {
        if (content?.ContentId == null) return string.Empty;
        return @$"{BracketCodePoints.Create(content)}";
    }

    public async Task DefaultBracketCodeToClipboard(PointContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = @$"{BracketCodePoints.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public async Task Delete(PointContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError($"Point {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        await Db.DeletePointContent(content.ContentId, StatusContext.ProgressTracker());

        var possibleContentDirectory = settings.LocalSitePointContentDirectory(content, false);
        if (possibleContentDirectory.Exists)
        {
            StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
            possibleContentDirectory.Delete(true);
        }
    }

    public async Task Edit(PointContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        var newContentWindow = await PointContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public async Task ExtractNewLinks(PointContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();

        var refreshedData = context.PointContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
    }

    public async Task GenerateHtml(PointContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        var fullItem = await Db.PointAndPointDetails(content.ContentId);

        if (fullItem == null)
        {
            StatusContext.ToastError("Item no longer exists in DB?");
            return;
        }

        var htmlContext = new SinglePointPage(fullItem);

        await htmlContext.WriteLocalHtml();

        StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
    }

    public async Task ViewOnSite(PointContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.PointPageUrl(content)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    public async Task ViewHistory(PointContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricPointContents.Where(x => x.ContentId == content.ContentId).ToListAsync();

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

    public static PointListListItem ListItemFromDbItem(PointContent content, PointContentActions itemActions,
        bool showType)
    {
        return new PointListListItem
        {
            DbEntry = content,
            SmallImageUrl = ContentListContext.GetSmallImageUrl(content),
            ItemActions = itemActions,
            ShowType = showType
        };
    }
}