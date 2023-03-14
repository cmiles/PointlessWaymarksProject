using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData.ContentHtml.NoteHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.NoteList;

public partial class NoteListWithActionsContext : ObservableObject
{
    [ObservableProperty] private CmsCommonCommands _commonCommands;
    [ObservableProperty] private RelayCommand _emailHtmlToClipboardCommand;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private RelayCommand _refreshDataCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus? _windowStatus;

    private NoteListWithActionsContext(StatusControlContext statusContext, WindowIconStatus? windowStatus, ContentListContext listContext, bool loadInBackground = true)
    {
        _statusContext = statusContext;
        _windowStatus = windowStatus;

        _listContext = listContext;

        _commonCommands = new CmsCommonCommands(StatusContext, WindowStatus);

        _refreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
        _emailHtmlToClipboardCommand = StatusContext.RunBlockingTaskCommand(EmailHtmlToClipboard);

        ListContext.ContextMenuItems = new List<ContextMenuItemData>
        {
            new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new()
            {
                ItemName = "Text Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new() { ItemName = "Email Html to Clipboard", ItemCommand = EmailHtmlToClipboardCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        if(loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public static async Task<NoteListWithActionsContext> CreateInstance(StatusControlContext? statusContext, WindowIconStatus? windowStatus = null, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryListContext = await ContentListContext.CreateInstance(factoryContext, new NoteListLoader(100), windowStatus);

        return new NoteListWithActionsContext(factoryContext, windowStatus, factoryListContext, loadInBackground);
    }

    private async Task EmailHtmlToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (SelectedItems().Count > 1)
        {
            StatusContext.ToastError("Please select only 1 item...");
            return;
        }

        var frozenSelected = SelectedItems().First();

        var emailHtml = await Email.ToHtmlEmail(frozenSelected.DbEntry, StatusContext.ProgressTracker());

        await ThreadSwitcher.ResumeForegroundAsync();

        HtmlClipboardHelpers.CopyToClipboard(emailHtml, emailHtml);

        StatusContext.ToastSuccess("Email Html on Clipboard");
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await ListContext.LoadData();
    }

    public List<NoteListListItem> SelectedItems()
    {
        return ListContext.ListSelection.SelectedItems?.Where(x => x is NoteListListItem).Cast<NoteListListItem>()
            .ToList() ?? new List<NoteListListItem>();
    }
}