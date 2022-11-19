using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using AngleSharp.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.Reports;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.LoggingTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using Point = NetTopologySuite.Geometries.Point;

namespace PointlessWaymarks.CmsWpfControls.PhotoList;

[ObservableObject]
public partial class PhotoListWithActionsContext
{
    [ObservableProperty] private RelayCommand _addIntersectionTagsToSelectedCommand;
    [ObservableProperty] private RelayCommand _dailyPhotoLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private RelayCommand _emailHtmlToClipboardCommand;
    [ObservableProperty] private RelayCommand _forcedResizeCommand;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private RelayCommand _openUrlForPhotoListCommand;
    [ObservableProperty] private RelayCommand _photoLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private RelayCommand _photoToPointContentEditorCommand;
    [ObservableProperty] private RelayCommand _refreshDataCommand;
    [ObservableProperty] private RelayCommand _regenerateHtmlAndReprocessPhotoForSelectedCommand;
    [ObservableProperty] private RelayCommand _reportAllPhotosCommand;
    [ObservableProperty] private RelayCommand _reportBlankLicenseCommand;
    [ObservableProperty] private RelayCommand _reportMultiSpacesInTitleCommand;
    [ObservableProperty] private RelayCommand _reportNoTagsCommand;
    [ObservableProperty] private RelayCommand _reportPhotoMetadataCommand;
    [ObservableProperty] private RelayCommand _reportTakenAndLicenseYearDoNotMatchCommand;
    [ObservableProperty] private RelayCommand _reportTitleAndTakenDoNotMatchCommand;
    [ObservableProperty] private RelayCommand _rescanMetadataAndFillBlanksCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand _viewFilesCommand;
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

    private async Task AddIntersectionTagsToSelected(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var frozenSelect = SelectedItems().ToList();

        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile))
        {
            StatusContext.ToastError("The Settings File for the Feature Intersection is blank?");
            return;
        }

        var settingsFileInfo = new FileInfo(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile);
        if (!settingsFileInfo.Exists)
        {
            StatusContext.ToastError(
                $"The Settings File for the Feature Intersection {settingsFileInfo.FullName} doesn't exist?");
            return;
        }

        var tagger = new Intersection();

        var errorList = new List<string>();
        var successList = new List<string>();
        var noTagsList = new List<string>();

        var processedCount = 0;

        cancellationToken.ThrowIfCancellationRequested();

        var toProcess = new List<(PhotoContent dbClone, IFeature pointFeature)>();

        foreach (var loopSelected in frozenSelect)
        {
            if (loopSelected.DbEntry.Latitude is null || loopSelected.DbEntry.Longitude is null) continue;

            toProcess.Add(((PhotoContent)new PhotoContent().InjectFrom(loopSelected.DbEntry),
                new Feature(new Point(loopSelected.DbEntry.Longitude.Value, loopSelected.DbEntry.Latitude.Value),
                    new AttributesTable())));
        }

        var tagReturn = tagger.Tags(settingsFileInfo.FullName, toProcess.Select(x => x.pointFeature).ToList(),
            cancellationToken, StatusContext.ProgressTracker());

        var updateTime = DateTime.Now;

        foreach (var loopSelected in toProcess)
        {
            processedCount++;

            try
            {
                var taggerResult = tagReturn.Single(x => x.Feature == loopSelected.pointFeature);

                if (!taggerResult.Tags.Any())
                {
                    noTagsList.Add($"{loopSelected.dbClone.Title} - no tags found");
                    StatusContext.Progress(
                        $"Processed - {loopSelected.dbClone.Title} - no tags found - Photo {processedCount} of {frozenSelect.Count}");
                    continue;
                }

                var tagListForIntersection = Db.TagListParse(loopSelected.dbClone.Tags);
                tagListForIntersection.AddRange(taggerResult.Tags);
                loopSelected.dbClone.Tags = Db.TagListJoin(tagListForIntersection);
                loopSelected.dbClone.LastUpdatedBy = "Feature Intersection Tagger";
                loopSelected.dbClone.LastUpdatedOn = updateTime;

                var (saveGenerationReturn, _) =
                    await PhotoGenerator.SaveAndGenerateHtml(loopSelected.dbClone,
                        UserSettingsSingleton.CurrentSettings().LocalSitePhotoContentFile(loopSelected.dbClone), false,
                        DateTime.Now, StatusContext.ProgressTracker());

                if (saveGenerationReturn.HasError)
                    //TODO: Need alerting on this that would actually be seen...
                {
                    Log.ForContext("generationError", saveGenerationReturn.GenerationNote)
                        .ForContext("generationException", saveGenerationReturn.Exception?.ToString() ?? string.Empty)
                        .Error(
                            "Photo Save Error during Selected Photo Feature Intersection Tagging");
                    errorList.Add(
                        $"Save Failed! Photo: {loopSelected.dbClone.Title}, {saveGenerationReturn.GenerationNote}");
                    continue;
                }

                successList.Add(
                    $"{loopSelected.dbClone.Title} - found Tags {string.Join(", ", taggerResult.Tags)}");
                StatusContext.Progress(
                    $"Processed - {loopSelected.dbClone.Title} - found Tags {string.Join(", ", taggerResult.Tags)} - Photo {processedCount} of {frozenSelect.Count}");
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"Photo Save Error during Selected Photo Feature Intersection Tagging {loopSelected.dbClone.Title}, {loopSelected.dbClone.ContentId}");
                errorList.Add(
                    $"Save Failed! Photo: {loopSelected.dbClone.Title}, {e.Message}");
            }

            if (cancellationToken.IsCancellationRequested) break;
        }

        if (errorList.Any())
        {
            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine(
                $"There were errors getting Feature Intersection Tags and saving items - Errors: {errorList.Count}, Success: {successList.Count}, No Tags: {noTagsList.Count}.");
            bodyBuilder.AppendLine();
            bodyBuilder.AppendFormat("Errors:");
            bodyBuilder.AppendLine(string.Join(Environment.NewLine, errorList));
            bodyBuilder.AppendLine();
            bodyBuilder.AppendFormat("Successes:");
            bodyBuilder.AppendLine(string.Join(Environment.NewLine, successList));
            bodyBuilder.AppendLine();
            bodyBuilder.AppendFormat("No Tags Found:");
            bodyBuilder.AppendLine(string.Join(Environment.NewLine, noTagsList));

            await StatusContext.ShowMessageWithOkButton("Feature Intersection Errors", bodyBuilder.ToString());
        }
    }

    private async Task DailyPhotoLinkCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = SelectedItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + BracketCodeDailyPhotoPage.Create(loopSelected.DbEntry) + Environment.NewLine);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
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

            PointlessWaymarksLogTools.LogGenerationReturn(resizeResult, "Photo Forced Resizing");

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
            new()
            {
                ItemName = "Daily Photo Page Code to Clipboard",
                ItemCommand = DailyPhotoLinkCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Email Html to Clipboard", ItemCommand = EmailHtmlToClipboardCommand },
            new() { ItemName = "Photos to Point Content Editors", ItemCommand = PhotoToPointContentEditorCommand },
            new() { ItemName = "View Photos", ItemCommand = ViewFilesCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new()
            {
                ItemName = "Rescan Metadata/Fill Blanks - Selected", ItemCommand = RescanMetadataAndFillBlanksCommand
            },
            new() { ItemName = "Process/Resize Selected", ItemCommand = ForcedResizeCommand },
            new() { ItemName = "Add Intersection Tags", ItemCommand = AddIntersectionTagsToSelectedCommand },
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
            (current, loopSelected) =>
                current + BracketCodePhotoLinks.Create(loopSelected.DbEntry) + Environment.NewLine);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    private async Task PhotoToPointContentEditor(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (SelectedItems().Count > 10)
            if (await StatusContext.ShowMessage("Too Many Editors?",
                    $"You are about to open {SelectedItems().Count} Point Content Editors - do you really want to do that?",
                    new List<string> { "Yes", "No" }) == "No")
                return;

        var count = 1;

        var frozenNow = DateTime.Now;

        foreach (var loopPhoto in SelectedItems())
        {
            cancellationToken.ThrowIfCancellationRequested();

            StatusContext.Progress(
                $"Opening Point Content Editor for '{loopPhoto.DbEntry.Title}' - {count++} of {SelectedItems().Count}");

            var newPartialPoint = new PointContent
            {
                BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
                Latitude = UserSettingsSingleton.CurrentSettings().LatitudeDefault,
                Longitude = UserSettingsSingleton.CurrentSettings().LongitudeDefault,
                CreatedOn = frozenNow,
                FeedOn = frozenNow,
                BodyContent = BracketCodePhotos.Create(loopPhoto.DbEntry),
                Title = $"Point From {loopPhoto.DbEntry.Title}",
                Tags = loopPhoto.DbEntry.Tags
            };

            newPartialPoint.Slug = SlugTools.Create(true, newPartialPoint.Title);

            if (loopPhoto.DbEntry.Latitude != null) newPartialPoint.Latitude = loopPhoto.DbEntry.Latitude.Value;
            if (loopPhoto.DbEntry.Longitude != null) newPartialPoint.Longitude = loopPhoto.DbEntry.Longitude.Value;
            if (loopPhoto.DbEntry.Elevation != null) newPartialPoint.Elevation = loopPhoto.DbEntry.Elevation.Value;

            var pointWindow = await PointContentEditorWindow.CreateInstance(newPartialPoint);

            await pointWindow.PositionWindowAndShowOnUiThread();
        }
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

    private async Task RescanMetadataAndFillBlanks(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var frozenSelected = SelectedItems().ToList();

        var errorMessages = new List<string>();
        var updates = new List<(string updateMessage, PhotoContent toUpdate)>();

        StatusContext.Progress($"Processing {frozenSelected.Count} Photo Files");

        var counter = 0;
        var updateSetTime = DateTime.Now;

        foreach (var loopSelected in frozenSelected)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StatusContext.Progress(
                $"Photo - {loopSelected.DbEntry.Title} - {++counter} of {frozenSelected.Count} - Checking Metadata");

            var mediaFile = UserSettingsSingleton.CurrentSettings()
                .LocalMediaArchivePhotoContentFile(loopSelected.DbEntry);

            if (mediaFile is not { Exists: true })
            {
                errorMessages.Add($"{loopSelected.DbEntry.Title} - Media File Not Found? Something has gone wrong...");
                continue;
            }

            var metadataReturn = await PhotoGenerator.PhotoMetadataFromFile(mediaFile, true, StatusContext.ProgressTracker());

            if (metadataReturn.generationReturn.HasError)
            {
                errorMessages.Add(
                    $"{loopSelected.DbEntry.Title} - error with metadata? {metadataReturn.generationReturn.GenerationNote}.");
                continue;
            }

            var toModify = (PhotoContent)new PhotoContent().InjectFrom(loopSelected.DbEntry);

            if (toModify.PhotoCreatedOnUtc == null && metadataReturn.metadata.PhotoCreatedOnUtc != null)
                toModify.PhotoCreatedOnUtc = metadataReturn.metadata.PhotoCreatedOnUtc;
            if (string.IsNullOrWhiteSpace(toModify.PhotoCreatedBy) &&
                !string.IsNullOrWhiteSpace(metadataReturn.metadata.PhotoCreatedBy))
                toModify.PhotoCreatedBy = metadataReturn.metadata.PhotoCreatedBy;
            if (string.IsNullOrWhiteSpace(toModify.Aperture) &&
                !string.IsNullOrWhiteSpace(metadataReturn.metadata.Aperture))
                toModify.Aperture = metadataReturn.metadata.Aperture;
            if (string.IsNullOrWhiteSpace(toModify.CameraMake) &&
                !string.IsNullOrWhiteSpace(metadataReturn.metadata.CameraMake))
                toModify.CameraMake = metadataReturn.metadata.CameraMake;
            if (string.IsNullOrWhiteSpace(toModify.CameraModel) &&
                !string.IsNullOrWhiteSpace(metadataReturn.metadata.CameraModel))
                toModify.CameraModel = metadataReturn.metadata.CameraModel;
            if (string.IsNullOrWhiteSpace(toModify.FocalLength) &&
                !string.IsNullOrWhiteSpace(metadataReturn.metadata.FocalLength))
                toModify.FocalLength = metadataReturn.metadata.FocalLength;
            if (string.IsNullOrWhiteSpace(toModify.Lens) &&
                !string.IsNullOrWhiteSpace(metadataReturn.metadata.Lens))
                toModify.Lens = metadataReturn.metadata.Lens;
            if (string.IsNullOrWhiteSpace(toModify.License) &&
                !string.IsNullOrWhiteSpace(metadataReturn.metadata.License))
                toModify.License = metadataReturn.metadata.License;
            if (string.IsNullOrWhiteSpace(toModify.ShutterSpeed) &&
                !string.IsNullOrWhiteSpace(metadataReturn.metadata.ShutterSpeed))
                toModify.ShutterSpeed = metadataReturn.metadata.ShutterSpeed;
            if (toModify.Iso == null && metadataReturn.metadata.Iso != null)
                toModify.Iso = metadataReturn.metadata.Iso;
            if (toModify.Latitude == null && metadataReturn.metadata.Latitude != null)
                toModify.Latitude = metadataReturn.metadata.Latitude;
            if (toModify.Longitude == null && metadataReturn.metadata.Longitude != null)
                toModify.Longitude = metadataReturn.metadata.Longitude;
            if (toModify.Elevation == null && metadataReturn.metadata.Elevation != null)
                toModify.Elevation = metadataReturn.metadata.Elevation;

            var comparisonResult =
                new CompareLogic { Config = { MaxDifferences = 100 } }.Compare(loopSelected.DbEntry, toModify);

            if (comparisonResult.AreEqual)
            {
                StatusContext.Progress($"Photo - {loopSelected.DbEntry.Title} - No Changes");
                continue;
            }

            toModify.LastUpdatedOn = updateSetTime;
            toModify.LastUpdatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;

            var friendlyReport = new UserFriendlyReport();
            updates.Add((friendlyReport.OutputString(comparisonResult.Differences), toModify));
        }

        if (errorMessages.Any() && !updates.Any())
        {
            await StatusContext.ShowMessageWithOkButton("Errors getting Metadata",
                $"No changes were found but there were errors during processing: {Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, errorMessages)}");
            return;
        }

        if (errorMessages.Any())
            if (await StatusContext.ShowMessage("Errors getting Metadata",
                    $"There were errors during processing, continue and see changes? {Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, errorMessages)}",
                    new List<string> { "Yes", "No" }) == "No")
                return;

        var changeMessages = string.Join(Environment.NewLine,
            updates.Select(x => $"{Environment.NewLine}{x.toUpdate.Title}{Environment.NewLine}{x.updateMessage}"));

        if (await StatusContext.ShowMessage("Metadata Updates",
                $"Update {updates.Count} Photos where blanks were replaced based on current Metadata? {Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, changeMessages)}",
                new List<string> { "Yes", "No" }) == "No") return;

        var updateErrorMessages = new List<string>();
        var successCount = 0;

        foreach (var loopUpdate in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await PhotoGenerator.SaveAndGenerateHtml(loopUpdate.toUpdate,
                UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(loopUpdate.toUpdate), false,
                DateTime.Now, StatusContext.ProgressTracker());

            if (result.generationReturn.HasError)
                updateErrorMessages.Add(
                    $"{loopUpdate.toUpdate.Title} - {loopUpdate.toUpdate.ContentId} - {result.generationReturn.GenerationNote}");
            else
                successCount++;
        }

        if (updateErrorMessages.Any())
            await StatusContext.ShowMessageWithOkButton("Errors Saving Photos",
                $"Saved {successCount} Photos successfully but there were {updateErrorMessages.Count} failures: {Environment.NewLine}{string.Join(Environment.NewLine, updateErrorMessages)}");
    }

    private static async Task RunReport(Func<Task<List<object>>> toRun, string title)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var reportLoader = new ContentListLoaderReport(toRun);

        var newWindow = await PhotoListWindow.CreateInstance(new PhotoListWithActionsContext(null, reportLoader));
        newWindow.WindowTitle = title;
        await newWindow.PositionWindowAndShowOnUiThread();
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
        DailyPhotoLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(DailyPhotoLinkCodesToClipboardForSelected);
        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
        ForcedResizeCommand = StatusContext.RunBlockingTaskWithCancellationCommand(ForcedResize, "Cancel Resizing");
        ViewFilesCommand = StatusContext.RunBlockingTaskWithCancellationCommand(ViewFilesSelected, "Cancel File View");
        PhotoToPointContentEditorCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(PhotoToPointContentEditor,
                "Cancel Photos to Point Editors");
        RescanMetadataAndFillBlanksCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(RescanMetadataAndFillBlanks, "Cancel Metadata Update");
        AddIntersectionTagsToSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(AddIntersectionTagsToSelected,
                "Cancel Feature Intersection Tagging");

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