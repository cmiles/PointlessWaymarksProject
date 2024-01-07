using System.Windows;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ImageList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ImageListWithActionsContext
{
    private ImageListWithActionsContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus,
        ContentListContext listContext, bool loadInBackground = true)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;
        CommonCommands = new CmsCommonCommands(StatusContext, WindowStatus);

        BuildCommands();

        ListContext = listContext;

        ListContext.ContextMenuItems =
        [
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
            new() { ItemName = "View Images", ItemCommand = ViewSelectedFilesCommand },
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
        ];

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(RefreshData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public ContentListContext ListContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }

    public static async Task<ImageListWithActionsContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus = null, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryContext, new ImageListLoader(100), windowStatus);

        return new ImageListWithActionsContext(factoryContext, windowStatus, factoryListContext, loadInBackground);
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
    private async Task ForcedResize(CancellationToken cancellationToken)
    {
        var totalCount = SelectedListItems().Count;
        var currentLoop = 0;

        foreach (var loopSelected in SelectedListItems())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (++currentLoop % 10 == 0)
                StatusContext.Progress($"Cleaning Generated Images And Resizing {currentLoop} of {totalCount} - " +
                                       $"{loopSelected.DbEntry.Title}");
            var resizeResult =
                await PictureResizing.CopyCleanResizeImage(loopSelected.DbEntry, StatusContext.ProgressTracker());

            if (!resizeResult.HasError) continue;

            PointlessWaymarksLogTools.LogGenerationReturn(resizeResult, "Image Forced Resizing");

            if (currentLoop < totalCount)
            {
                if (await StatusContext.ShowMessage("Error Resizing",
                        $"There was an error resizing the image {loopSelected.DbEntry.OriginalFileName} in {loopSelected.DbEntry.Title}{Environment.NewLine}{Environment.NewLine}{resizeResult.GenerationNote}{Environment.NewLine}{Environment.NewLine}Continue?",
                        ["Yes", "No"]) == "No") return;
            }
            else
            {
                await StatusContext.ShowMessageWithOkButton("Error Resizing",
                    $"There was an error resizing the image {loopSelected.DbEntry.OriginalFileName} in {loopSelected.DbEntry.Title}{Environment.NewLine}{Environment.NewLine}{resizeResult.GenerationNote}");
            }
        }
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task ImageBracketLinkCodesToClipboardForSelected()
    {
        var finalString = SelectedListItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + $"{BracketCodeImageLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard: {finalString}");
    }

    [BlockingCommand]
    private async Task RefreshData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await ListContext.LoadData();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task RegenerateHtmlAndReprocessImageForSelected(CancellationToken cancellationToken)
    {
        var loopCount = 0;
        var totalCount = SelectedListItems().Count;

        var db = await Db.Context();

        var errorList = new List<string>();

        foreach (var loopSelected in SelectedListItems())
        {
            if (cancellationToken.IsCancellationRequested) break;

            loopCount++;

            if (loopSelected.DbEntry.Id < 1)
            {
                StatusContext.Progress(
                    $"Re-processing Image and Generating Html for {loopCount} of {totalCount} failed - no saved DB Entry?");
                errorList.Add("There was a list item without a saved DB entry? This should never happen...");
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

            var localMediaFiles = UserSettingsSingleton.CurrentSettings()
                .LocalMediaArchiveImageContentFile(currentVersion);

            if (localMediaFiles == null)
            {
                StatusContext.Progress(
                    $"Re-processing Image and Generating Html for {loopSelected.DbEntry.Title} failed - file not found in Media Library, {loopCount} of {totalCount}");
                errorList.Add($"Image Titled {loopSelected.DbEntry.Title} was not found in the Media Library?");
                continue;
            }

            var (generationReturn, _) = await ImageGenerator.SaveAndGenerateHtml(currentVersion, localMediaFiles
                , true, null,
                StatusContext.ProgressTracker());

            if (generationReturn.HasError)
            {
                PointlessWaymarksLogTools.LogGenerationReturn(generationReturn,
                    "Error with Image Resizing and HTML Regeneration");
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

    public List<ImageListListItem> SelectedListItems()
    {
        return ListContext.ListSelection.SelectedItems?.Where(x => x is ImageListListItem).Cast<ImageListListItem>()
            .ToList() ?? [];
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 10)]
    public async Task ViewSelectedFiles(CancellationToken cancelToken)
    {
        var currentSelected = SelectedListItems();

        foreach (var loopSelected in currentSelected)
        {
            cancelToken.ThrowIfCancellationRequested();

            await loopSelected.ItemActions.ViewFile(loopSelected.DbEntry);
        }
    }
}