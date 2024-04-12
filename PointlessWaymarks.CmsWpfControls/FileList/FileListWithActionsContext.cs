using System.Windows;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.FileList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FileListWithActionsContext : IListSelectionWithContext<FileListListItem>
{
    private FileListWithActionsContext(StatusControlContext statusContext, WindowIconStatus? windowStatus,
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
                ItemName = "Image Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },

            new ContextMenuItemData
            {
                ItemName = "Text Code to Clipboard",
                ItemCommand = FilePageLinkCodesToClipboardForSelectedCommand
            },

            new ContextMenuItemData
            {
                ItemName = "Download Code to Clipboard",
                ItemCommand = FileDownloadLinkCodesToClipboardForSelectedCommand
            },

            new ContextMenuItemData
            {
                ItemName = "Embed Code to Clipboard",
                ItemCommand = FileEmbedCodesToClipboardForSelectedCommand
            },

            new ContextMenuItemData
            {
                ItemName = "URL Code to Clipboard", ItemCommand = FileUrlLinkCodesToClipboardForSelectedCommand
            },

            new ContextMenuItemData
            {
                ItemName = "Picture Gallery to Clipboard",
                ItemCommand = ListContext.PictureGalleryBracketCodeToClipboardSelectedCommand
            },

            new ContextMenuItemData { ItemName = "Email Html to Clipboard", ItemCommand = EmailHtmlToClipboardCommand },
            new ContextMenuItemData { ItemName = "View Files", ItemCommand = ViewSelectedFilesCommand },
            new ContextMenuItemData { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new ContextMenuItemData
                { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new ContextMenuItemData
                { ItemName = "Generate Html", ItemCommand = ListContext.GenerateHtmlSelectedCommand },
            new ContextMenuItemData { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new ContextMenuItemData { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new ContextMenuItemData
            {
                ItemName = "Map Selected Items", ItemCommand = ListContext.SpatialItemsToContentMapWindowSelectedCommand
            },
            new ContextMenuItemData { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        ];

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(RefreshData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public ContentListContext ListContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }

    public FileListListItem? SelectedListItem()
    {
        return ListContext.ListSelection.Selected as FileListListItem;
    }

    public List<FileListListItem> SelectedListItems()
    {
        return ListContext.ListSelection.SelectedItems.Where(x => x is FileListListItem).Cast<FileListListItem>()
            .ToList();
    }

    public StatusControlContext StatusContext { get; set; }

    public static async Task<FileListWithActionsContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryStatusContext = statusContext ?? new StatusControlContext();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryStatusContext, new FileListLoader(100),
                [Db.ContentTypeDisplayStringForFile], windowStatus);

        return new FileListWithActionsContext(factoryStatusContext, windowStatus, factoryListContext, loadInBackground);
    }

    [BlockingCommand]
    [StopAndWarnIfNotOneSelectedListItems]
    private async Task EmailHtmlToClipboard()
    {
        var frozenSelected = SelectedListItems().First();

        var emailHtml = await Email.ToHtmlEmail(frozenSelected.DbEntry, StatusContext.ProgressTracker());

        await ThreadSwitcher.ResumeForegroundAsync();

        HtmlClipboardHelpers.CopyToClipboard(emailHtml, emailHtml);

        StatusContext.ToastSuccess("Email Html on Clipboard");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task FileDownloadLinkCodesToClipboardForSelected()
    {
        var finalString = string.Empty;

        foreach (var loopSelected in SelectedListItems())
            finalString += $"{BracketCodeFileDownloads.Create(loopSelected.DbEntry)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task FileEmbedCodesToClipboardForSelected()
    {
        var finalString = string.Empty;

        foreach (var loopSelected in SelectedListItems())
            finalString += $"{BracketCodeFileEmbed.Create(loopSelected.DbEntry)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task FilePageLinkCodesToClipboardForSelected()
    {
        var finalString = string.Empty;

        foreach (var loopSelected in SelectedListItems())
            finalString += $"{BracketCodeFiles.Create(loopSelected.DbEntry)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task FileUrlLinkCodesToClipboardForSelected()
    {
        var finalString = string.Empty;

        foreach (var loopSelected in SelectedListItems())
            finalString += $"{BracketCodeFileUrl.Create(loopSelected.DbEntry)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 10)]
    private async Task FirstPagePreviewFromPdf()
    {
        var frozenSelected = SelectedListItems();

        await ImageExtractionHelpers.PdfPageToImage(StatusContext, frozenSelected.Select(x => x.DbEntry).ToList(), 1);
    }

    [BlockingCommand]
    private async Task RefreshData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await ListContext.LoadData();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 10)]
    public async Task ViewSelectedFiles(CancellationToken cancelToken)
    {
        var frozenSelected = SelectedListItems();

        foreach (var loopSelected in frozenSelected)
        {
            cancelToken.ThrowIfCancellationRequested();
            await loopSelected.ItemActions.ViewFile(loopSelected.DbEntry);
        }
    }
}