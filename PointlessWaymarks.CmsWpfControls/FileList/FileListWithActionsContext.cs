using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.FileList;

[ObservableObject]
public partial class FileListWithActionsContext
{
    [ObservableProperty] private RelayCommand _emailHtmlToClipboardCommand;
    [ObservableProperty] private RelayCommand _fileDownloadLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private RelayCommand _fileEmbedCodesToClipboardForSelectedCommand;
    [ObservableProperty] private RelayCommand _filePageLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private RelayCommand _fileUrlLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private RelayCommand _firstPagePreviewFromPdfToCairoCommand;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private RelayCommand _refreshDataCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand _viewFilesCommand;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public FileListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    private async Task EmailHtmlToClipboard()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
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

    private async Task FileDownloadLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
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

    private async Task FileEmbedCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
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

    private async Task FilePageLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
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

    private async Task FileUrlLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
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

    private async Task FirstPagePreviewFromPdfToCairo()
    {
        var selected = SelectedItems();

        if (selected == null || !selected.Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        await ImageExtractionHelpers.PdfPageToImageWithPdfToCairo(StatusContext, selected.Select(x => x.DbEntry).ToList(), 1);
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new FileListLoader(100), WindowStatus);

        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
        FilePageLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(FilePageLinkCodesToClipboardForSelected);
        FileDownloadLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(FileDownloadLinkCodesToClipboardForSelected);
        FileEmbedCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(FileEmbedCodesToClipboardForSelected);
        FileUrlLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(FileDownloadLinkCodesToClipboardForSelected);
        ViewFilesCommand = StatusContext.RunBlockingTaskWithCancellationCommand(ViewFilesSelected, "Cancel File View");

        FirstPagePreviewFromPdfToCairoCommand = StatusContext.RunBlockingTaskCommand(FirstPagePreviewFromPdfToCairo);

        EmailHtmlToClipboardCommand = StatusContext.RunBlockingTaskCommand(EmailHtmlToClipboard);

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
            new() { ItemName = "View Files", ItemCommand = ViewFilesCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Generate Html", ItemCommand = ListContext.GenerateHtmlSelectedCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    public List<FileListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is FileListListItem).Cast<FileListListItem>()
            .ToList() ?? new List<FileListListItem>();
    }

    public async Task ViewFilesSelected(CancellationToken cancelToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (ListContext.ListSelection?.SelectedItems == null || ListContext.ListSelection.SelectedItems.Count < 1)
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