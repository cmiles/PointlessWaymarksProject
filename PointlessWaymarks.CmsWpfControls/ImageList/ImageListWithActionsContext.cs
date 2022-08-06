using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ImageList;

[ObservableObject]
public partial class ImageListWithActionsContext
{
    [ObservableProperty] private RelayCommand _emailHtmlToClipboardCommand;
    [ObservableProperty] private RelayCommand _forcedResizeCommand;
    [ObservableProperty] private RelayCommand _imageBracketLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private RelayCommand _refreshDataCommand;
    [ObservableProperty] private RelayCommand _regenerateHtmlAndReprocessImageForSelectedCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand _viewFilesCommand;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public ImageListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
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

    private async Task ForcedResize(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var totalCount = SelectedItems().Count;
        var currentLoop = 0;

        foreach (var loopSelected in SelectedItems())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (++currentLoop % 10 == 0)
                StatusContext.Progress($"Cleaning Generated Images And Resizing {currentLoop} of {totalCount} - " +
                                       $"{loopSelected.DbEntry.Title}");
            var resizeResult =
                await PictureResizing.CopyCleanResizeImage(loopSelected.DbEntry, StatusContext.ProgressTracker());

            if (!resizeResult.HasError) continue;

            LogHelpers.LogGenerationReturn(resizeResult, "Image Forced Resizing");

            if (currentLoop < totalCount)
            {
                if (await StatusContext.ShowMessage("Error Resizing",
                        $"There was an error resizing the image {loopSelected.DbEntry.OriginalFileName} in {loopSelected.DbEntry.Title}{Environment.NewLine}{Environment.NewLine}{resizeResult.GenerationNote}{Environment.NewLine}{Environment.NewLine}Continue?",
                        new List<string> { "Yes", "No" }) == "No") return;
            }
            else
            {
                await StatusContext.ShowMessageWithOkButton("Error Resizing",
                    $"There was an error resizing the image {loopSelected.DbEntry.OriginalFileName} in {loopSelected.DbEntry.Title}{Environment.NewLine}{Environment.NewLine}{resizeResult.GenerationNote}");
            }
        }
    }

    private async Task ImageBracketLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = SelectedItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + @$"{BracketCodeImageLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard: {finalString}");
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new ImageListLoader(100), WindowStatus);

        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);

        ImageBracketLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(ImageBracketLinkCodesToClipboardForSelected);

        ForcedResizeCommand = StatusContext.RunBlockingTaskWithCancellationCommand(ForcedResize, "Cancel Resizing");
        RegenerateHtmlAndReprocessImageForSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(RegenerateHtmlAndReprocessImageForSelected,
                "Cancel HTML Generation and Image Resizing");

        ViewFilesCommand = StatusContext.RunBlockingTaskWithCancellationCommand(ViewFilesSelected, "Cancel File View");

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
                ItemCommand = ImageBracketLinkCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Email Html to Clipboard", ItemCommand = EmailHtmlToClipboardCommand },
            new() { ItemName = "View Images", ItemCommand = ViewFilesCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Process/Resize Selected", ItemCommand = ForcedResizeCommand },
            new()
            {
                ItemName = "Generate Html/Process/Resize Selected",
                ItemCommand = RegenerateHtmlAndReprocessImageForSelectedCommand
            },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    private async Task RegenerateHtmlAndReprocessImageForSelected(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var loopCount = 0;
        var totalCount = SelectedItems().Count;

        var db = await Db.Context();

        var errorList = new List<string>();

        foreach (var loopSelected in SelectedItems())
        {
            if (cancellationToken.IsCancellationRequested) break;

            loopCount++;

            if (loopSelected.DbEntry == null)
            {
                StatusContext.Progress(
                    $"Re-processing Image and Generating Html for {loopCount} of {totalCount} failed - no DB Entry?");
                errorList.Add("There was a list item without a DB entry? This should never happen...");
                continue;
            }

            var currentVersion = db.ImageContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

            if (currentVersion == null)
            {
                StatusContext.Progress(
                    $"Re-processing Image and Generating Html for {loopSelected.DbEntry.Title} failed - not found in DB, {loopCount} of {totalCount}");
                errorList.Add($"Image Titled {loopSelected.DbEntry.Title} was not found in the database?");
                continue;
            }

            if (string.IsNullOrWhiteSpace(currentVersion.LastUpdatedBy))
                currentVersion.LastUpdatedBy = currentVersion.CreatedBy;
            currentVersion.LastUpdatedOn = DateTime.Now;

            StatusContext.Progress(
                $"Re-processing Image and Generating Html for {loopSelected.DbEntry.Title}, {loopCount} of {totalCount}");

            var (generationReturn, _) = await ImageGenerator.SaveAndGenerateHtml(currentVersion,
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveImageContentFile(currentVersion), true, null,
                StatusContext.ProgressTracker());

            if (generationReturn.HasError)
            {
                LogHelpers.LogGenerationReturn(generationReturn, "Error with Image Resizing and HTML Regeneration");
                StatusContext.Progress(
                    $"Re-processing Image and Generating Html for {loopSelected.DbEntry.Title} Error {generationReturn.GenerationNote}, {generationReturn.Exception}, {loopCount} of {totalCount}");
                errorList.Add(
                    $"Error processing Image Titled {loopSelected.DbEntry.Title} - {generationReturn.GenerationNote}");
            }
        }

        if (errorList.Any())
        {
            errorList.Reverse();
            await StatusContext.ShowMessageWithOkButton("Errors Resizing and Regenerating HTML",
                string.Join($"{Environment.NewLine}{Environment.NewLine}", errorList));
        }
    }

    public List<ImageListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is ImageListListItem).Cast<ImageListListItem>()
            .ToList() ?? new List<ImageListListItem>();
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

            if (loopSelected is ImageListListItem imageItem)
                await imageItem.ItemActions.ViewFile(imageItem.DbEntry);
        }
    }
}