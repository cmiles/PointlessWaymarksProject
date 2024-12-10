using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.ContentHtml.LineMonthlyActivitySummaryHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.FeatureIntersectionTags.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.CmsWpfControls.LineList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LineListWithActionsContext
{
    private LineListWithActionsContext(StatusControlContext statusContext, WindowIconStatus? windowStatus,
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
                ItemName = "Map Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new ContextMenuItemData
                { ItemName = "Text Code to Clipboard", ItemCommand = LinkBracketCodesToClipboardForSelectedCommand },
            new ContextMenuItemData
            {
                ItemName = "Stats Text Code to Clipboard",
                ItemCommand = TextStatsBracketCodesToClipboardForSelectedCommand
            },
            new ContextMenuItemData
            {
                ItemName = "Stats Block Code to Clipboard", ItemCommand = StatsBracketCodesToClipboardForSelectedCommand
            },
            new ContextMenuItemData
            {
                ItemName = "Elevation Chart Code to Clipboard",
                ItemCommand = ElevationChartBracketCodesToClipboardForSelectedCommand
            },
            new ContextMenuItemData
            {
                ItemName = "Picture Gallery to Clipboard",
                ItemCommand = ListContext.PictureGalleryBracketCodeToClipboardSelectedCommand
            },
            new ContextMenuItemData
            {
                ItemName = "GeoJson to Clipboard", ItemCommand = GeoJsonToClipboardForSelectedCommand
            },
            new ContextMenuItemData
            {
                ItemName = "Save Selected to Gpx File - Single File", ItemCommand = SelectedToGpxFileCommand
            },
            new ContextMenuItemData
            {
                ItemName = "Save Selected to Gpx File - Individual Files", ItemCommand = SelectedToGpxFilesCommand
            },
            new ContextMenuItemData
            {
                ItemName = "Activity Log Monthly Stats Window",
                ItemCommand = ActivityLogMonthlyStatsWindowForSelectedCommand
            },
            new ContextMenuItemData
                { ItemName = "Add Intersection Tags", ItemCommand = AddIntersectionTagsToSelectedCommand },
            new ContextMenuItemData
                { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new ContextMenuItemData { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new ContextMenuItemData { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new ContextMenuItemData
            {
                ItemName = "Re-Save Selected", ItemCommand = ResaveSelectedCommand
            },
            new ContextMenuItemData { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new ContextMenuItemData
            {
                ItemName = "Map Selected Items", ItemCommand = ListContext.SpatialItemsToContentMapWindowSelectedCommand
            },
            new ContextMenuItemData
            {
                ItemName = "View Selected Pictures", ItemCommand = ListContext.PicturesAndVideosViewWindowSelectedCommand
            },
            new ContextMenuItemData { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        ];

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(RefreshData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public ContentListContext ListContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }

    [BlockingCommand]
    private async Task ActivityLogMonthlyStatsWindowForAllLineContent()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        var allActivities =
            await db.LineContents.LineContentFilteredForActivities().Select(x => x.ContentId).ToListAsync();

        var window =
            await ActivityLogMonthlySummaryWindow.CreateInstance(allActivities);

        await window.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task ActivityLogMonthlyStatsWindowForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var frozenSelected = SelectedListItems();

        var window =
            await ActivityLogMonthlySummaryWindow.CreateInstance(frozenSelected.Select(x => x.DbEntry.ContentId)
                .ToList());

        await window.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task AddIntersectionTagsToSelected(CancellationToken cancellationToken)
    {
        var frozenSelect = SelectedListItems();

        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile))
        {
            await StatusContext.ToastError("The Settings File for the Feature Intersection is blank?");
            return;
        }

        var settingsFileInfo = new FileInfo(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile);
        if (!settingsFileInfo.Exists)
        {
            await StatusContext.ToastError(
                $"The Settings File for the Feature Intersection {settingsFileInfo.FullName} doesn't exist?");
            return;
        }

        var errorList = new List<string>();
        var successList = new List<string>();
        var noTagsList = new List<string>();

        var processedCount = 0;

        cancellationToken.ThrowIfCancellationRequested();

        List<LineContent> dbEntriesToProcess = [];
        List<IntersectResult> intersectResults = [];

        foreach (var loopSelected in frozenSelect)
        {
            var features = loopSelected.DbEntry.FeatureFromGeoJsonLine();

            if (features == null) continue;

            dbEntriesToProcess.Add((LineContent)LineContent.CreateInstance().InjectFrom(loopSelected.DbEntry));
            intersectResults.Add(new IntersectResult(features)
                { ContentId = loopSelected.DbEntry.ContentId });
        }

        intersectResults.IntersectionTags(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
            cancellationToken, StatusContext.ProgressTracker());

        var updateTime = DateTime.Now;

        foreach (var loopSelected in dbEntriesToProcess)
        {
            processedCount++;

            try
            {
                var taggerResult = intersectResults.Single(x => x.ContentId == loopSelected.ContentId);

                if (!taggerResult.Tags.Any())
                {
                    noTagsList.Add($"{loopSelected.Title} - no tags found");
                    StatusContext.Progress(
                        $"Processed - {loopSelected.Title} - no tags found - Line {processedCount} of {frozenSelect.Count}");
                    continue;
                }

                var tagListForIntersection = Db.TagListParse(loopSelected.Tags);
                tagListForIntersection.AddRange(taggerResult.Tags);
                loopSelected.Tags = Db.TagListJoin(tagListForIntersection);
                loopSelected.LastUpdatedBy = "Feature Intersection Tagger";
                loopSelected.LastUpdatedOn = updateTime;

                var (saveGenerationReturn, _) =
                    await LineGenerator.SaveAndGenerateHtml(loopSelected, DateTime.Now,
                        StatusContext.ProgressTracker());

                if (saveGenerationReturn.HasError)
                    //TODO: Need alerting on this that would actually be seen...
                {
                    Log.ForContext("generationError", saveGenerationReturn.GenerationNote)
                        .ForContext("generationException", saveGenerationReturn.Exception?.ToString() ?? string.Empty)
                        .Error(
                            "Line Save Error during Selected Line Feature Intersection Tagging");
                    errorList.Add(
                        $"Save Failed! Line: {loopSelected.Title}, {saveGenerationReturn.GenerationNote}");
                    continue;
                }

                successList.Add(
                    $"{loopSelected.Title} - found Tags {string.Join(", ", taggerResult.Tags)}");
                StatusContext.Progress(
                    $"Processed - {loopSelected.Title} - found Tags {string.Join(", ", taggerResult.Tags)} - Line {processedCount} of {frozenSelect.Count}");
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"Line Save Error during Selected Line Feature Intersection Tagging {loopSelected.Title}, {loopSelected.ContentId}");
                errorList.Add(
                    $"Save Failed! Line: {loopSelected.Title}, {e.Message}");
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

    public static async Task<LineListWithActionsContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus = null, bool loadInBackground = true)
    {
        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        await ThreadSwitcher.ResumeBackgroundAsync();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryStatusContext, new LineListLoader(100),
                [Db.ContentTypeDisplayStringForLine], windowStatus);

        return new LineListWithActionsContext(factoryStatusContext, windowStatus, factoryListContext, loadInBackground);
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task ElevationChartBracketCodesToClipboardForSelected()
    {
        var finalString = SelectedListItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + $"{BracketCodeLineElevationCharts.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        await StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 3, ActionVerb = "copy to clipboard")]
    private async Task GeoJsonToClipboardForSelected()
    {
        var frozenSelected = SelectedListItems();

        var featureList = new List<IFeature>();
        var warningList = new List<string>();
        var successCounter = 0;

        foreach (var loopSelected in frozenSelected)
        {
            var lineFeature = loopSelected.DbEntry.FeatureFromGeoJsonLine();

            if (lineFeature is null)
            {
                warningList.Add(loopSelected.DbEntry.Title ?? "Unknown");
                continue;
            }

            featureList.Add(lineFeature);
            successCounter++;
        }

        var finalString = await GeoJsonTools.SerializeListOfFeaturesCollectionToGeoJson(featureList);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        if (successCounter > 0)
            await StatusContext.ToastSuccess($"GeoJson To Clipboard for {successCounter} Lines");

        if (warningList.Any())
            await StatusContext.ShowMessageWithOkButton("GeoJson Conversion Failures?",
                $"GeoJson Conversion failed for {warningList.Count} items.{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, warningList)}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task LineStatsToExcelForSelected()
    {
        var selectedItems = SelectedListItems();
        StatusContext.Progress($"Starting transfer of {selectedItems.Count} to Excel");

        var file = Path.Combine(FileLocationTools.TempStorageDirectory().FullName,
            $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}---{FileAndFolderTools.TryMakeFilenameValid("LineStatistics")}.xlsx");

        var settings = UserSettingsSingleton.CurrentSettings();

        var projectedItems = selectedItems.Select(x => new
        {
            x.DbEntry.Folder,
            x.DbEntry.Title,
            x.DbEntry.LineDistance,
            x.DbEntry.ClimbElevation,
            x.DbEntry.DescentElevation,
            x.DbEntry.MinimumElevation,
            x.DbEntry.MaximumElevation,
            x.DbEntry.Tags,
            Url = settings.LinePageUrl(x.DbEntry)
        });

        StatusContext.Progress($"File Name: {file}");

        StatusContext.Progress("Creating Workbook");

        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Exported Data");

        StatusContext.Progress("Inserting Data");

        var table = ws.Cell(1, 1).InsertTable(projectedItems);

        StatusContext.Progress("Applying Formatting");

        foreach (var loopRow in table.DataRange.Rows())
            loopRow.Cell(2).SetHyperlink(new XLHyperlink(loopRow.Cell(9).GetString()));

        table.DataRange.Column(3).Style.NumberFormat.Format = "#0.0";
        table.DataRange.Column(4).Style.NumberFormat.Format = "#,##0";
        table.DataRange.Column(5).Style.NumberFormat.Format = "#,##0";
        table.DataRange.Column(6).Style.NumberFormat.Format = "#,##0";
        table.DataRange.Column(7).Style.NumberFormat.Format = "#,##0";
        table.Column(9).Delete();

        ws.Columns().AdjustToContents();

        foreach (var loopColumn in ws.ColumnsUsed().Where(x => x.Width > 70))
        {
            loopColumn.Width = 70;
            loopColumn.Style.Alignment.WrapText = true;
        }

        ws.Rows().AdjustToContents();

        foreach (var loopRow in ws.RowsUsed().Where(x => x.Height > 70))
            loopRow.Height = 70;

        StatusContext.Progress($"Saving Excel File {file}");

        wb.SaveAs(file);

        StatusContext.Progress($"Opening Excel File {file}");

        var ps = new ProcessStartInfo(file) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task LinkBracketCodesToClipboardForSelected()
    {
        var finalString = SelectedListItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + $"{BracketCodeLineLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        await StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    private async Task RefreshData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await ListContext.LoadData();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task ResaveSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var selectedIds = SelectedListItems().Select(x => x.ContentId()).Where(x => x is not null).ToList();

        var db = await Db.Context().ConfigureAwait(false);

        var selectedToSave = await db.LineContents.Where(x => selectedIds.Contains(x.ContentId)).OrderBy(x => x.Title)
            .ToListAsync().ConfigureAwait(false);

        var totalCount = selectedToSave.Count;

        StatusContext.Progress($"Found {totalCount} Lines to Generate");

        var generationVersion = DateTime.Now.TrimDateTimeToSeconds().ToUniversalTime();
        

        await Parallel.ForEachAsync(selectedToSave, async (loopItem, _) =>
        {
            StatusContext.Progress($"Saving and Writing HTML for Line {loopItem.Title}");

            await LineGenerator.SaveAndGenerateHtml(loopItem, generationVersion, StatusContext.ProgressTracker());
        }).ConfigureAwait(false);

        await MapComponentGenerator.GenerateAllLinesData();
        await new LineMonthlyActivitySummaryPage(generationVersion).WriteLocalHtml();
    }

    public List<LineListListItem> SelectedListItems()
    {
        return ListContext.ListSelection.SelectedItems.Where(x => x is LineListListItem).Cast<LineListListItem>()
            .ToList() ?? [];
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task SelectedToGpxFile()
    {
        var frozenSelected = SelectedListItems();

        await ThreadSwitcher.ResumeForegroundAsync();

        var fileDialog = new VistaSaveFileDialog
        {
            Filter = "gpx file (*.gpx)|*.gpx;",
            AddExtension = true,
            OverwritePrompt = true,
            DefaultExt = ".gpx"
        };
        var fileDialogResult = fileDialog.ShowDialog();

        if (!(fileDialogResult ?? false)) return;

        var fileName = fileDialog.FileName;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var trackList = frozenSelected.Select(x => GpxTools.GpxTrackFromLineFeature(x.DbEntry.FeatureFromGeoJsonLine()!,
            x.DbEntry.RecordingStartedOnUtc, x.DbEntry.Title ?? "New Track", string.Empty,
            x.DbEntry.Title!.Replace(".", string.Empty)
                .Contains(x.DbEntry.Summary.TrimNullToEmpty().Replace(".", string.Empty),
                    StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : x.DbEntry.Summary ?? string.Empty)).ToList();

        var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);

        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, CloseOutput = true };
        await using var xmlWriter = XmlWriter.Create(fileStream, writerSettings);
        GpxWriter.Write(xmlWriter, null, new GpxMetadata("Pointless Waymarks CMS"), null, null, trackList, null);
        xmlWriter.Close();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task SelectedToGpxFiles()
    {
        var frozenSelected = SelectedListItems();

        await ThreadSwitcher.ResumeForegroundAsync();

        var fileDialog = new VistaFolderBrowserDialog() { Multiselect = false };
        var fileDialogResult = fileDialog.ShowDialog();

        if (!(fileDialogResult ?? false)) return;

        var directory = new DirectoryInfo(fileDialog.SelectedPath);

        if (!directory.Exists)
        {
            await StatusContext.ToastError("Directory doesn't exist?");
            return;
        }

        await ThreadSwitcher.ResumeBackgroundAsync();

        foreach (var loopSelected in frozenSelected)
        {
            var trackList = GpxTools.GpxTrackFromLineFeature(loopSelected.DbEntry.FeatureFromGeoJsonLine()!,
                loopSelected.DbEntry.RecordingStartedOnUtc, loopSelected.DbEntry.Title ?? "New Track", string.Empty,
                loopSelected.DbEntry.Summary ?? string.Empty).AsList();

            var fileName = UniqueFileTools.UniqueFile(directory, $"{loopSelected.DbEntry.Title!}.gpx");

            if (fileName is null)
            {
                await StatusContext.ToastError($"Couldn't create a unique file name for {loopSelected.DbEntry.Title}?");
                continue;
            }

            var fileStream = new FileStream(fileName.FullName, FileMode.OpenOrCreate);

            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, CloseOutput = true };
            await using var xmlWriter = XmlWriter.Create(fileStream, writerSettings);
            GpxWriter.Write(xmlWriter, null, new GpxMetadata("Pointless Waymarks CMS"), null, null, trackList, null);
            xmlWriter.Close();
        }
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task StatsBracketCodesToClipboardForSelected()
    {
        var finalString = SelectedListItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + $"{BracketCodeLineStats.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        await StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task TextStatsBracketCodesToClipboardForSelected()
    {
        var finalString = SelectedListItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + $"{BracketCodeLineTextStats.Create(loopSelected.DbEntry)}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        await StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }
}