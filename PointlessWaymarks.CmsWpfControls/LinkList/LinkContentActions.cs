using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml.LinkListHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LinkList;

public partial class LinkContentActions : ObservableObject, IContentActions<LinkContent>
{
    [ObservableProperty] private RelayCommand<string> _copyUrlCommand;
    [ObservableProperty] private RelayCommand<LinkContent> _deleteCommand;
    [ObservableProperty] private RelayCommand<LinkContent> _editCommand;
    [ObservableProperty] private RelayCommand<LinkContent> _extractNewLinksCommand;
    [ObservableProperty] private RelayCommand<LinkContent> _generateHtmlCommand;
    [ObservableProperty] private RelayCommand<LinkContent> _linkCodeToClipboardCommand;
    [ObservableProperty] private RelayCommand<LinkContent> _viewOnSiteCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand<LinkContent> _viewHistoryCommand;

    public LinkContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        DeleteCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(Delete);
        EditCommand = StatusContext.RunNonBlockingTaskCommand<LinkContent>(Edit);
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(ExtractNewLinks);
        GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(GenerateHtml);
        LinkCodeToClipboardCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(DefaultBracketCodeToClipboard);
        ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand<LinkContent>(ViewOnSite);
        ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<LinkContent>(ViewHistory);

        CopyUrlCommand = StatusContext.RunNonBlockingTaskCommand<string>(async x =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            Clipboard.SetText(x);

            StatusContext.ToastSuccess($"To Clipboard {x}");
        });
    }

    public string DefaultBracketCode(LinkContent content)
    {
        return content?.ContentId == null ? string.Empty : $"[{content.Title}]({content.Url})";
    }

    public async Task DefaultBracketCodeToClipboard(LinkContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = $"[{content.Title}]({content.Url})";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public async Task Delete(LinkContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError($"Link {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        await Db.DeleteLinkContent(content.ContentId, StatusContext.ProgressTracker());
    }

    public async Task Edit(LinkContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.LinkContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        var newContentWindow = await LinkContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public async Task ExtractNewLinks(LinkContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();

        var refreshedData = context.LinkContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{refreshedData.Comments} {refreshedData.Description}", StatusContext.ProgressTracker());
    }

    public async Task GenerateHtml(LinkContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Generating Html for Link List");

        var htmlContext = new LinkListPage();

        await htmlContext.WriteLocalHtmlRssAndJson();

        var settings = UserSettingsSingleton.CurrentSettings();

        StatusContext.ToastSuccess($"Generated {settings.LinkListUrl()}");
    }

    public async Task ViewOnSite(LinkContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (string.IsNullOrWhiteSpace(content.Url))
        {
            StatusContext.ToastError("URL is Blank?");
            return;
        }

        var url = content.Url;

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    public async Task ViewHistory(LinkContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricLinkContents.Where(x => x.ContentId == content.ContentId).ToListAsync();

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

    public static LinkListListItem ListItemFromDbItem(LinkContent content, LinkContentActions itemActions,
        bool showType)
    {
        return new LinkListListItem { DbEntry = content, ItemActions = itemActions, ShowType = showType };
    }
}