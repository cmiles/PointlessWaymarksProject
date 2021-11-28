using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using AngleSharp.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PhotoList;

[ObservableObject]
public partial class PhotoListWithActionsContext
{
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private Command _emailHtmlToClipboardCommand;
    [ObservableProperty] private Command _forcedResizeCommand;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private Command _openUrlForPhotoListCommand;
    [ObservableProperty] private Command _photoLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private Command _refreshDataCommand;
    [ObservableProperty] private Command _regenerateHtmlAndReprocessPhotoForSelectedCommand;
    [ObservableProperty] private Command _reportAllPhotosCommand;
    [ObservableProperty] private Command _reportBlankLicenseCommand;
    [ObservableProperty] private Command _reportMultiSpacesInTitleCommand;
    [ObservableProperty] private Command _reportNoTagsCommand;
    [ObservableProperty] private Command _reportPhotoMetadataCommand;
    [ObservableProperty] private Command _reportTakenAndLicenseYearDoNotMatchCommand;
    [ObservableProperty] private Command _reportTitleAndTakenDoNotMatchCommand;
    [ObservableProperty] private Command _viewFilesCommand;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public PhotoListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public PhotoListWithActionsContext(StatusControlContext statusContext, IContentListLoader reportFilter)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        ListContext ??= new ContentListContext(StatusContext, reportFilter);

        SetupCommands();

        StatusContext.RunFireAndForgetBlockingTask(ListContext.LoadData);
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
                await PictureResizing.CopyCleanResizePhoto(loopSelected.DbEntry, StatusContext.ProgressTracker());

            if (!resizeResult.HasError) continue;

            LogHelpers.LogGenerationReturn(resizeResult, "Photo Forced Resizing");

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


    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new PhotoListLoader(100), WindowStatus);

        SetupCommands();

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
                ItemName = "Text Code to Clipboard", ItemCommand = PhotoLinkCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Email Html to Clipboard", ItemCommand = EmailHtmlToClipboardCommand },
            new() { ItemName = "View Photos", ItemCommand = ViewFilesCommand },
            new() { ItemName = "Open URLs", ItemCommand = ListContext.OpenUrlSelectedCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Process/Resize Selected", ItemCommand = ForcedResizeCommand },
            new()
            {
                ItemName = "Generate Html/Process/Resize Selected",
                ItemCommand = RegenerateHtmlAndReprocessPhotoForSelectedCommand
            },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    private async Task PhotoLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = SelectedItems().Aggregate(string.Empty,
            (current, loopSelected) => current + BracketCodePhotos.Create(loopSelected.DbEntry) + Environment.NewLine);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    private async Task RegenerateHtmlAndReprocessPhotoForSelected(CancellationToken cancellationToken)
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
                    $"Re-processing Photo and Generating Html for {loopCount} of {totalCount} failed - no DB Entry?");
                errorList.Add("There was a list item without a DB entry? This should never happen...");
                continue;
            }

            var currentVersion = db.PhotoContents.SingleOrDefault(x => x.ContentId == loopSelected.DbEntry.ContentId);

            if (currentVersion == null)
            {
                StatusContext.Progress(
                    $"Re-processing Photo and Generating Html for {loopSelected.DbEntry.Title} failed - not found in DB, {loopCount} of {totalCount}");
                errorList.Add($"Photo Titled {loopSelected.DbEntry.Title} was not found in the database?");
                continue;
            }

            if (string.IsNullOrWhiteSpace(currentVersion.LastUpdatedBy))
                currentVersion.LastUpdatedBy = currentVersion.CreatedBy;
            currentVersion.LastUpdatedOn = DateTime.Now;

            StatusContext.Progress(
                $"Re-processing Photo and Generating Html for {loopSelected.DbEntry.Title}, {loopCount} of {totalCount}");

            var (generationReturn, _) = await PhotoGenerator.SaveAndGenerateHtml(currentVersion,
                UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(currentVersion), true, null,
                StatusContext.ProgressTracker());

            if (generationReturn.HasError)
            {
                StatusContext.Progress(
                    $"Re-processing Photo and Generating Html for {loopSelected.DbEntry.Title} Error {generationReturn.GenerationNote}, {generationReturn.Exception}, {loopCount} of {totalCount}");
                errorList.Add($"Error processing Photo Titled {loopSelected.DbEntry.Title}...");
            }
        }

        if (errorList.Any())
        {
            errorList.Reverse();
            await StatusContext.ShowMessageWithOkButton("Errors Resizing and Regenerating HTML",
                string.Join($"{Environment.NewLine}{Environment.NewLine}", errorList));
        }
    }

    private async Task<List<object>> ReportAllPhotosGenerator()
    {
        var db = await Db.Context();

        return (await db.PhotoContents.OrderByDescending(x => x.PhotoCreatedOn).ToListAsync()).Cast<object>().ToList();
    }

    private async Task<List<object>> ReportBlankLicenseGenerator()
    {
        var db = await Db.Context();

        var allContents = await db.PhotoContents.OrderByDescending(x => x.PhotoCreatedOn).ToListAsync();

        var returnList = new List<PhotoContent>();

        foreach (var loopContents in allContents)
            if (string.IsNullOrWhiteSpace(loopContents.License))
                returnList.Add(loopContents);

        return returnList.Cast<object>().ToList();
    }

    private async Task<List<object>> ReportMultiSpacesInTitleGenerator()
    {
        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.Title.Contains("  ")).OrderByDescending(x => x.PhotoCreatedOn)
            .ToListAsync()).Cast<object>().ToList();
    }

    private async Task<List<object>> ReportNoTagsGenerator()
    {
        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.Tags == "").ToListAsync()).Cast<object>().ToList();
    }

    private async Task ReportPhotoMetadata()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var selected = SelectedItems();

        if (selected == null || !selected.Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (selected.Count > 1)
        {
            StatusContext.ToastError("Please Select a Single Item");
            return;
        }

        var singleSelected = selected.Single();

        var archiveFile = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoDirectory().ToString(),
            singleSelected.DbEntry.OriginalFileName));

        await PhotoMetadataReport.AllPhotoMetadataToHtml(archiveFile, StatusContext);
    }

    private async Task<List<object>> ReportTakenAndLicenseYearDoNotMatchGenerator()
    {
        var db = await Db.Context();

        var allContents = await db.PhotoContents.OrderByDescending(x => x.PhotoCreatedOn).ToListAsync();

        var returnList = new List<PhotoContent>();

        foreach (var loopContents in allContents)
        {
            if (string.IsNullOrWhiteSpace(loopContents.License))
            {
                returnList.Add(loopContents);
                continue;
            }

            var possibleYear = Regex.Match(loopContents.License, @"(?<PossibleYear>[12]\d\d\d)",
                RegexOptions.IgnoreCase).Value;

            if (string.IsNullOrWhiteSpace(possibleYear)) continue;

            if (!int.TryParse(possibleYear, out var licenseYear)) continue;

            var createdOn = loopContents.PhotoCreatedOn.Year;

            if (createdOn == licenseYear) continue;

            returnList.Add(loopContents);
        }

        return returnList.Cast<object>().ToList();
    }

    private async Task<List<object>> ReportTitleAndTakenDoNotMatchGenerator()
    {
        var db = await Db.Context();

        var allContents = await db.PhotoContents.OrderByDescending(x => x.PhotoCreatedOn).ToListAsync();

        var returnList = new List<PhotoContent>();

        foreach (var loopContents in allContents)
        {
            if (string.IsNullOrWhiteSpace(loopContents.Title)) continue;

            var splitName = loopContents.Title.Split(" ");

            if (splitName.Length < 2) continue;

            if (!splitName[0].All(x => x.IsDigit())) continue;

            if (!int.TryParse(splitName[0], out var titleYear)) continue;

            var dateInfo = new DateTimeFormatInfo();

            if (!dateInfo.MonthNames.Contains(splitName[1])) continue;

            var titleMonth = dateInfo.MonthNames.ToList().IndexOf(splitName[1]) + 1;

            if (titleYear == loopContents.PhotoCreatedOn.Year &&
                titleMonth == loopContents.PhotoCreatedOn.Month) continue;

            returnList.Add(loopContents);
        }

        return returnList.Cast<object>().ToList();
    }

    private static async Task RunReport(Func<Task<List<object>>> toRun, string title)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var reportLoader = new ContentListLoaderReport(toRun);

        var context = new PhotoListWithActionsContext(null, reportLoader);

        await ThreadSwitcher.ResumeForegroundAsync();

        var newWindow = new PhotoListWindow { PhotoListContext = context, WindowTitle = title };

        newWindow.PositionWindowAndShow();

        await context.LoadData();
    }

    public List<PhotoListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is PhotoListListItem).Cast<PhotoListListItem>()
            .ToList() ?? new List<PhotoListListItem>();
    }

    private void SetupCommands()
    {
        RegenerateHtmlAndReprocessPhotoForSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(RegenerateHtmlAndReprocessPhotoForSelected,
                "Cancel HTML Generation and Photo Resizing");
        PhotoLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(PhotoLinkCodesToClipboardForSelected);
        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
        ForcedResizeCommand = StatusContext.RunBlockingTaskWithCancellationCommand(ForcedResize, "Cancel Resizing");
        ViewFilesCommand = StatusContext.RunBlockingTaskWithCancellationCommand(ViewFilesSelected, "Cancel File View");

        EmailHtmlToClipboardCommand = StatusContext.RunBlockingTaskCommand(EmailHtmlToClipboard);

        ReportPhotoMetadataCommand = StatusContext.RunBlockingTaskCommand(ReportPhotoMetadata);
        ReportNoTagsCommand = StatusContext.RunBlockingTaskCommand(async () =>
            await RunReport(ReportNoTagsGenerator, "No Tags Photo List"));
        ReportTitleAndTakenDoNotMatchCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            await RunReport(ReportTitleAndTakenDoNotMatchGenerator, "Title and Created Mismatch Photo List"));
        ReportTakenAndLicenseYearDoNotMatchCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            await RunReport(ReportTakenAndLicenseYearDoNotMatchGenerator, "Title and Created Mismatch Photo List"));
        ReportAllPhotosCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            await RunReport(ReportAllPhotosGenerator, "Title and Created Mismatch Photo List"));
        ReportBlankLicenseCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            await RunReport(ReportBlankLicenseGenerator, "Title and Created Mismatch Photo List"));
        ReportMultiSpacesInTitleCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
            await RunReport(ReportMultiSpacesInTitleGenerator, "Title with Multiple Spaces"));
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

            if (loopSelected is PhotoListListItem photoItem)
                await photoItem.ItemActions.ViewFile(photoItem.DbEntry);
        }
    }
}