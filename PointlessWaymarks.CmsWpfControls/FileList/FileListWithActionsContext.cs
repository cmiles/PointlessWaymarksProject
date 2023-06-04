using System.Windows;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.FileList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FileListWithActionsContext
{
    private FileListWithActionsContext(StatusControlContext statusContext, WindowIconStatus? windowStatus,
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
                ItemName = "Image Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new()
            {
                ItemName = "Text Code to Clipboard",
                ItemCommand = FilePageLinkCodesToClipboardForSelectedCommand
            },
            new()
            {
                ItemName = "Download Code to Clipboard",
                ItemCommand = FileDownloadLinkCodesToClipboardForSelectedCommand
            },
            new()
            {
                ItemName = "Embed Code to Clipboard",
                ItemCommand = FileEmbedCodesToClipboardForSelectedCommand
            },
            new()
            {
                ItemName = "URL Code to Clipboard", ItemCommand = FileUrlLinkCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Email Html to Clipboard", ItemCommand = EmailHtmlToClipboardCommand },
            new() { ItemName = "View Files", ItemCommand = ViewSelectedFilesCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Generate Html", ItemCommand = ListContext.GenerateHtmlSelectedCommand },
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

    public static async Task<FileListWithActionsContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryStatusContext = statusContext ?? new StatusControlContext();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryStatusContext, new FileListLoader(100), windowStatus);

        return new FileListWithActionsContext(factoryStatusContext, windowStatus, factoryListContext, loadInBackground);
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
    private async Task FileDownloadLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = string.Empty;

        foreach (var loopSelected in SelectedItems())
            finalString += @$"{BracketCodeFileDownloads.Create(loopSelected.DbEntry)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    private async Task FileEmbedCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = string.Empty;

        foreach (var loopSelected in SelectedItems())
            finalString += @$"{BracketCodeFileEmbed.Create(loopSelected.DbEntry)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    private async Task FilePageLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = string.Empty;

        foreach (var loopSelected in SelectedItems())
            finalString += @$"{BracketCodeFiles.Create(loopSelected.DbEntry)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    private async Task FileUrlLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = string.Empty;

        foreach (var loopSelected in SelectedItems())
            finalString += @$"{BracketCodeFileUrl.Create(loopSelected.DbEntry)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    private async Task FirstPagePreviewFromPdf()
    {
        var selected = SelectedItems();

        if (!selected.Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        await ImageExtractionHelpers.PdfPageToImage(StatusContext, selected.Select(x => x.DbEntry).ToList(), 1);
    }

    [BlockingCommand]

    private async Task RefreshData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await ListContext.LoadData();
    }

    public List<FileListListItem> SelectedItems()
    {
        return ListContext.ListSelection.SelectedItems?.Where(x => x is FileListListItem).Cast<FileListListItem>()
            .ToList() ?? new List<FileListListItem>();
    }

    [BlockingCommand]
    public async Task ViewSelectedFiles(CancellationToken cancelToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ListContext.ListSelection.SelectedItems == null || ListContext.ListSelection.SelectedItems.Count < 1)
        {
            StatusContext.ToastWarning("Nothing Selected to View?");
            return;
        }

        if (ListContext.ListSelection.SelectedItems.Count > 20)
        {
            StatusContext.ToastWarning("Sorry - please select less than 20 items to view...");
            return;
        }

        var currentSelected = ListContext.ListSelection.SelectedItems;

        foreach (var loopSelected in currentSelected)
        {
            cancelToken.ThrowIfCancellationRequested();

            if (loopSelected is FileListListItem fileItem)
                await fileItem.ItemActions.ViewFile(fileItem.DbEntry);
        }
    }
}