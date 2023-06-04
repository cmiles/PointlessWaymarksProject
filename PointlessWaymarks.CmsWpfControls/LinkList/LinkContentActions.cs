using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.ContentHtml.LinkListHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LinkList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LinkContentActions : IContentActions<LinkContent>
{
    public LinkContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        BuildCommands();
    }

    public string DefaultBracketCode(LinkContent? content)
    {
        return content?.ContentId == null ? string.Empty : $"[{content.Title}]({content.Url})";
    }

    [BlockingCommand]
    public async Task DefaultBracketCodeToClipboard(LinkContent? content)
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

    [BlockingCommand]
    public async Task Delete(LinkContent? content)
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

    [NonBlockingCommand]
    public async Task Edit(LinkContent? content)
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

    [BlockingCommand]
    public async Task ExtractNewLinks(LinkContent? content)
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

    [BlockingCommand]
    public async Task GenerateHtml(LinkContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Generating Html for Link List");

        var htmlContext = new LinkListPage();

        await htmlContext.WriteLocalHtmlRssAndJson();

        var settings = UserSettingsSingleton.CurrentSettings();

        StatusContext.ToastSuccess($"Generated {settings.LinkListUrl()}");
    }

    public StatusControlContext StatusContext { get; set; }

    [NonBlockingCommand]
    public async Task ViewHistory(LinkContent? content)
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
                .Select(LogTools.SafeObjectDump).ToList());

        historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task ViewOnSite(LinkContent? content)
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task CopyUrl(string? link)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        if (string.IsNullOrWhiteSpace(link))
        {
            StatusContext.ToastError("Nothing to Copy?");
            return;
        }

        Clipboard.SetText(link);

        StatusContext.ToastSuccess($"To Clipboard {link}");
    }

    public static async Task<LinkListListItem> ListItemFromDbItem(LinkContent content, LinkContentActions itemActions,
        bool showType)
    {
        var item = await LinkListListItem.CreateInstance(itemActions);
        item.DbEntry = content;
        item.ShowType = showType;
        return item;
    }
}