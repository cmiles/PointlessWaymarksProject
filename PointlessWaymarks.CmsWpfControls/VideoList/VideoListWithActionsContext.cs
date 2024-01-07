using System.Windows;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.VideoHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.VideoList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class VideoListWithActionsContext
{
    private VideoListWithActionsContext(StatusControlContext statusContext, WindowIconStatus? windowStatus,
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
                ItemName = "Embed Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new()
            {
                ItemName = "Image Code to Clipboard",
                ItemCommand = VideoCoverImageLinkCodesToClipboardForSelectedCommand
            },
            new()
            {
                ItemName = "Text Code to Clipboard",
                ItemCommand = VideoPageLinkCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Email Html to Clipboard", ItemCommand = EmailHtmlToClipboardCommand },
            new() { ItemName = "View Videos", ItemCommand = ViewSelectedVideosCommand },
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

    public static async Task<VideoListWithActionsContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus = null, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryContext, new VideoListLoader(100), windowStatus);

        return new VideoListWithActionsContext(factoryContext, windowStatus, factoryListContext, loadInBackground);
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
    private async Task RefreshData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await ListContext.LoadData();
    }

    public List<VideoListListItem> SelectedListItems()
    {
        return ListContext.ListSelection.SelectedItems?.Where(x => x is VideoListListItem).Cast<VideoListListItem>()
            .ToList() ?? new List<VideoListListItem>();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task VideoPageLinkCodesToClipboardForSelected()
    {
        var finalString = string.Empty;

        foreach (var loopSelected in SelectedListItems())
            finalString += $"{BracketCodeVideoLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task VideoCoverImageLinkCodesToClipboardForSelected()
    {
        var finalString = string.Empty;

        bool showNoImageWarning = false;

        foreach (var loopSelected in SelectedListItems())
        {
            if (loopSelected.DbEntry.MainPicture == null)
            {
                showNoImageWarning = true;
                finalString += $"{BracketCodeVideoLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}";
            }
            else
            {
                finalString += $"{BracketCodeVideoImage.Create(loopSelected.DbEntry)}{Environment.NewLine}";
            }
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        if (showNoImageWarning)
        {
            StatusContext.ToastWarning("Not all Videos had a main image - some bracket codes are text links...");
        }
        else
        {
            StatusContext.ToastSuccess($"To Clipboard {finalString}");
        }
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 10)]
    public async Task ViewSelectedVideos(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

        foreach (var loopSelected in currentSelected)
        {
            cancelToken.ThrowIfCancellationRequested();

            await loopSelected.ItemActions.ViewFile(loopSelected.DbEntry);
        }
    }
}