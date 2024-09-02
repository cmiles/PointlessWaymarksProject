using System.Windows;
using HtmlTableHelper;
using pinboard.net;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.LinkList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LinkListWithActionsContext
{
    private LinkListWithActionsContext(StatusControlContext statusContext, WindowIconStatus? windowStatus,
        ContentListContext listContext, bool loadInBackground = true)
    {
        StatusContext = statusContext;
        WindowStatus = windowStatus;
        CommonCommands = new CmsCommonCommands(StatusContext, WindowStatus);

        BuildCommands();

        ListContext = listContext;

        ListContext.ContextMenuItems =
        [
            new ContextMenuItemData { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new ContextMenuItemData
            {
                ItemName = "[] Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },

            new ContextMenuItemData
                { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new ContextMenuItemData { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new ContextMenuItemData { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new ContextMenuItemData { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new ContextMenuItemData { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        ];

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(RefreshData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public ContentListContext ListContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }

    public static async Task<LinkListWithActionsContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus = null, bool loadInBackground = true)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryListContext =
            await ContentListContext.CreateInstance(factoryStatusContext, new LinkListLoader(100),
                [Db.ContentTypeDisplayStringForLink], windowStatus);

        return new LinkListWithActionsContext(factoryStatusContext, windowStatus, factoryListContext, loadInBackground);
    }

    [BlockingCommand]
    public async Task ListSelectedLinksNotOnPinboard()
    {
        await ListSelectedLinksNotOnPinboard(StatusContext.ProgressTracker());
    }

    private async Task ListSelectedLinksNotOnPinboard(IProgress<string>? progress)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().PinboardApiToken))
        {
            progress?.Report("No Pinboard Api Token... Can't check Pinboard.");
            return;
        }

        var selected = SelectedListItems().ToList();

        if (!selected.Any())
        {
            progress?.Report("Nothing Selected?");
            return;
        }

        progress?.Report($"Found {selected.Count} items to check.");


        using var pb = new PinboardAPI(UserSettingsSingleton.CurrentSettings().PinboardApiToken);

        var notFoundList = new List<LinkContent>();

        foreach (var loopSelected in selected)
        {
            if (string.IsNullOrWhiteSpace(loopSelected.DbEntry.Url))
            {
                notFoundList.Add(loopSelected.DbEntry);
                progress?.Report(
                    $"Link titled {loopSelected.DbEntry.Title} created on {loopSelected.DbEntry.CreatedOn:d} added because of blank URL...");
                continue;
            }

            var matches = await pb.Posts.Get(null, null, loopSelected.DbEntry.Url);

            if (!matches.Posts.Any())
            {
                progress?.Report(
                    $"Not Found Link titled {loopSelected.DbEntry.Title} created on {loopSelected.DbEntry.CreatedOn:d}");
                notFoundList.Add(loopSelected.DbEntry);
            }
            else
            {
                progress?.Report(
                    $"Found Link titled {loopSelected.DbEntry.Title} created on {loopSelected.DbEntry.CreatedOn:d}");
            }
        }

        if (!notFoundList.Any())
        {
            await StatusContext.ShowMessageWithOkButton("Pinboard Match Complete",
                $"Found a match on Pinboard for all {selected.Count} Selected links.");
            return;
        }

        progress?.Report($"Building table of {notFoundList.Count} items not found on Pinboard");

        var projectedNotFound = notFoundList.Select(x => new
        {
            x.Title,
            x.Url,
            x.CreatedBy,
            x.CreatedOn,
            x.LastUpdatedBy,
            x.LastUpdatedOn
        }).ToHtmlTable(new { @class = "pure-table pure-table-striped" });

        var htmlReportWindow = await WebViewWindow.CreateInstance();
        await htmlReportWindow.PositionWindowAndShowOnUiThread();
        await htmlReportWindow.SetupDocumentWithPureCss(projectedNotFound, "Links Not In Pinboard");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task MdLinkCodesToClipboardForSelected()
    {
        var finalString = string.Join(", ", SelectedListItems().Select(x => $"[{x.DbEntry.Title}]({x.DbEntry.Url})"));

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        await StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    private async Task RefreshData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await ListContext.LoadData();
    }

    public List<LinkListListItem> SelectedListItems()
    {
        return ListContext.ListSelection.SelectedItems.Where(x => x is LinkListListItem).Cast<LinkListListItem>()
            .ToList() ?? [];
    }
}