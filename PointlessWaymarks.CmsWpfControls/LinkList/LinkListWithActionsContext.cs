using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using HtmlTableHelper;
using JetBrains.Annotations;
using pinboard.net;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.HtmlViewer;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LinkList;

public class LinkListWithActionsContext : INotifyPropertyChanged
{
    private readonly StatusControlContext _statusContext;
    private ContentListContext _listContext;
    private Command _listSelectedLinksNotOnPinboardCommand;
    private Command _postCodesToClipboardForSelectedCommand;
    private Command _refreshDataCommand;
    private WindowIconStatus _windowStatus;

    public LinkListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public ContentListContext ListContext
    {
        get => _listContext;
        set
        {
            if (Equals(value, _listContext)) return;
            _listContext = value;
            OnPropertyChanged();
        }
    }

    public Command ListSelectedLinksNotOnPinboardCommand
    {
        get => _listSelectedLinksNotOnPinboardCommand;
        set
        {
            if (Equals(value, _listSelectedLinksNotOnPinboardCommand)) return;
            _listSelectedLinksNotOnPinboardCommand = value;
            OnPropertyChanged();
        }
    }

    public Command MdLinkCodesToClipboardForSelectedCommand
    {
        get => _postCodesToClipboardForSelectedCommand;
        set
        {
            if (Equals(value, _postCodesToClipboardForSelectedCommand)) return;
            _postCodesToClipboardForSelectedCommand = value;
            OnPropertyChanged();
        }
    }

    public Command RefreshDataCommand
    {
        get => _refreshDataCommand;
        set
        {
            if (Equals(value, _refreshDataCommand)) return;
            _refreshDataCommand = value;
            OnPropertyChanged();
        }
    }


    public StatusControlContext StatusContext
    {
        get => _statusContext;
        private init
        {
            if (Equals(value, _statusContext)) return;
            _statusContext = value;
            OnPropertyChanged();
        }
    }

    public WindowIconStatus WindowStatus
    {
        get => _windowStatus;
        set
        {
            if (Equals(value, _windowStatus)) return;
            _windowStatus = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private async Task ListSelectedLinksNotOnPinboard(IProgress<string> progress)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().PinboardApiToken))
        {
            progress?.Report("No Pinboard Api Token... Can't check Pinboard.");
            return;
        }

        var selected = SelectedItems().ToList();

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

        await ThreadSwitcher.ResumeForegroundAsync();

        var htmlReportWindow =
            new HtmlViewerWindow(
                projectedNotFound.ToHtmlDocumentWithPureCss("Links Not In Pinboard", string.Empty));
        htmlReportWindow.PositionWindowAndShow();
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new LinkListLoader(100), WindowStatus);

        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
        MdLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(MdLinkCodesToClipboardForSelected);

        ListSelectedLinksNotOnPinboardCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await ListSelectedLinksNotOnPinboard(StatusContext.ProgressTracker()));

        ListContext.ContextMenuItems = new List<ContextMenuItemData>
        {
            new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new()
            {
                ItemName = "[] Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.OpenUrlSelectedCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    private async Task MdLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = string.Join(", ", SelectedItems().Select(x => $"[{x.DbEntry.Title}]({x.DbEntry.Url})"));

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public List<LinkListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is LinkListListItem).Cast<LinkListListItem>()
            .ToList() ?? new List<LinkListListItem>();
    }
}