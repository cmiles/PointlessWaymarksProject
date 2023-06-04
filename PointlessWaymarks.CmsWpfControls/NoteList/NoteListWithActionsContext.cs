using PointlessWaymarks.CmsData.ContentHtml.NoteHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.NoteList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class NoteListWithActionsContext
{
    private NoteListWithActionsContext(StatusControlContext statusContext, WindowIconStatus? windowStatus,
        ContentListContext listContext, bool loadInBackground = true)
    {
        StatusContext = statusContext;
        WindowStatus = windowStatus;
        CommonCommands = new CmsCommonCommands(StatusContext, WindowStatus);

        BuildCommands();

        ListContext = listContext;

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

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(RefreshData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public ContentListContext ListContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }

    public static async Task<NoteListWithActionsContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus = null, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryContext, new NoteListLoader(100), windowStatus);

        return new NoteListWithActionsContext(factoryContext, windowStatus, factoryListContext, loadInBackground);
    }

    [BlockingCommand]
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

    [BlockingCommand]
    private async Task RefreshData()
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