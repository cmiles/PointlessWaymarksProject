using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
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
                ItemName = "Stats Code to Clipboard", ItemCommand = StatsBracketCodesToClipboardForSelectedCommand
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
                ItemName = "Save Gpx File", ItemCommand = SelectedToGpxFileCommand
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
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryContext, new LineListLoader(100),
                [Db.ContentTypeDisplayStringForLine], windowStatus);

        return new LineListWithActionsContext(factoryContext, windowStatus, factoryListContext, loadInBackground);
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
            x.DbEntry.Summary ?? string.Empty)).ToList();

        var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);

        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, CloseOutput = true };
        await using var xmlWriter = XmlWriter.Create(fileStream, writerSettings);
        GpxWriter.Write(xmlWriter, null, new GpxMetadata("Pointless Waymarks CMS"), null, null, trackList, null);
        xmlWriter.Close();
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
}