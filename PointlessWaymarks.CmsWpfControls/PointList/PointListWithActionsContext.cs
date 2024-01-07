using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
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

namespace PointlessWaymarks.CmsWpfControls.PointList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class PointListWithActionsContext
{
    private PointListWithActionsContext(StatusControlContext statusContext, WindowIconStatus? windowStatus,
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
                ItemName = "Map Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new()
            {
                ItemName = "Text Code to Clipboard",
                ItemCommand = PointLinkBracketCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Add Intersection Tags", ItemCommand = AddIntersectionTagsToSelectedCommand },
            new() { ItemName = "Selected Points to GPX File", ItemCommand = SelectedToGpxFileCommand },
            new()
            {
                ItemName = "Selected Points to Clipboard - GeoJson", ItemCommand = GeoJsonToClipboardForSelectedCommand
            },
            new() { ItemName = "Selected Points to Clipboard - Text", ItemCommand = ToClipboardForSelectedCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Map Selected Items", ItemCommand = ListContext.SpatialItemsToContentMapWindowSelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(RefreshData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public ContentListContext ListContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task AddIntersectionTagsToSelected(CancellationToken cancellationToken)
    {
        var frozenSelect = SelectedListItems();

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

        var errorList = new List<string>();
        var successList = new List<string>();
        var noTagsList = new List<string>();

        var processedCount = 0;

        cancellationToken.ThrowIfCancellationRequested();

        var pointDtos =
            await Db.PointAndPointDetails(frozenSelect.Select(x => x.DbEntry.ContentId).ToList(), await Db.Context());

        var toProcess = new List<PointContentDto>();
        var intersectResults = new List<IntersectResult>();

        foreach (var loopSelected in pointDtos)
        {
            var feature = loopSelected.FeatureFromPoint();

            toProcess.Add(loopSelected);
            intersectResults.Add(new IntersectResult(feature) { ContentId = loopSelected.ContentId });
        }

        intersectResults.IntersectionTags(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
            cancellationToken,
            StatusContext.ProgressTracker());

        var updateTime = DateTime.Now;

        foreach (var loopSelected in toProcess)
        {
            processedCount++;

            try
            {
                var taggerResult = intersectResults.Single(x => x.ContentId == loopSelected.ContentId);

                if (!taggerResult.Tags.Any())
                {
                    noTagsList.Add($"{loopSelected.Title} - no tags found");
                    StatusContext.Progress(
                        $"Processed - {loopSelected.Title} - no tags found - Point {processedCount} of {frozenSelect.Count}");
                    continue;
                }

                var tagListForIntersection = Db.TagListParse(loopSelected.Tags);
                tagListForIntersection.AddRange(taggerResult.Tags);
                loopSelected.Tags = Db.TagListJoin(tagListForIntersection);
                loopSelected.LastUpdatedBy = "Feature Intersection Tagger";
                loopSelected.LastUpdatedOn = updateTime;

                var (saveGenerationReturn, _) =
                    await PointGenerator.SaveAndGenerateHtml(loopSelected, DateTime.Now,
                        StatusContext.ProgressTracker());

                if (saveGenerationReturn.HasError)
                    //TODO: Need alerting on this that would actually be seen...
                {
                    Log.ForContext("generationError", saveGenerationReturn.GenerationNote)
                        .ForContext("generationException", saveGenerationReturn.Exception?.ToString() ?? string.Empty)
                        .Error(
                            "Point Save Error during Selected Point Feature Intersection Tagging");
                    errorList.Add(
                        $"Save Failed! Point: {loopSelected.Title}, {saveGenerationReturn.GenerationNote}");
                    continue;
                }

                successList.Add(
                    $"{loopSelected.Title} - found Tags {string.Join(", ", taggerResult.Tags)}");
                StatusContext.Progress(
                    $"Processed - {loopSelected.Title} - found Tags {string.Join(", ", taggerResult.Tags)} - Point {processedCount} of {frozenSelect.Count}");
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"Point Save Error during Selected Point Feature Intersection Tagging {loopSelected.Title}, {loopSelected.ContentId}");
                errorList.Add(
                    $"Save Failed! Point: {loopSelected.Title}, {e.Message}");
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

    public static async Task<PointListWithActionsContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus = null, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryListContext =
            await ContentListContext.CreateInstance(factoryContext, new PointListLoader(100), windowStatus);

        return new PointListWithActionsContext(factoryContext, windowStatus, factoryListContext, loadInBackground);
    }

    [BlockingCommand]
    private async Task RefreshData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await ListContext.LoadData();
    }

    [NonBlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task PointLinkBracketCodesToClipboardForSelected()
    {
        var finalString = SelectedListItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + $"{BracketCodePointLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public List<PointListListItem> SelectedListItems()
    {
        return ListContext.ListSelection.SelectedItems?.Where(x => x is PointListListItem).Cast<PointListListItem>()
            .ToList() ?? new List<PointListListItem>();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task SelectedToGpxFile()
    {
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

        var waypointList = new List<GpxWaypoint>();

        foreach (var loopItems in SelectedListItems())
        {
            var toAdd = new GpxWaypoint(new GpxLongitude(loopItems.DbEntry.Longitude),
                new GpxLatitude(loopItems.DbEntry.Latitude),
                loopItems.DbEntry.Elevation,
                loopItems.DbEntry.LastUpdatedOn?.ToUniversalTime() ?? loopItems.DbEntry.CreatedOn.ToUniversalTime(),
                null, null,
                loopItems.DbEntry.Title, null, loopItems.DbEntry.Summary, null, new ImmutableArray<GpxWebLink>(), null,
                null, null, null, null, null, null, null, null, null);
            waypointList.Add(toAdd);
        }

        var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);

        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, CloseOutput = true };
        await using var xmlWriter = XmlWriter.Create(fileStream, writerSettings);
        GpxWriter.Write(xmlWriter, null, new GpxMetadata("Pointless Waymarks CMS"), waypointList, null, null, null);
        xmlWriter.Close();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItemsAskIfOverMax(MaxSelectedItems = 100, ActionVerb = "copy to clipboard")]
    private async Task GeoJsonToClipboardForSelected()
    {
        var frozenSelected = SelectedListItems();

        var featureList = new List<IFeature>();

        foreach (var loopSelected in frozenSelected)
        {
            var pointFeature = loopSelected.DbEntry.FeatureFromPoint();
            featureList.Add(pointFeature);
        }

        var finalString = await GeoJsonTools.SerializeListOfFeaturesCollectionToGeoJson(featureList);

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"GeoJson Points To Clipboard for {frozenSelected.Count} Points");
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task ToClipboardForSelected()
    {
        var frozenSelected = SelectedListItems();

        var pointList = new StringBuilder();

        foreach (var loopSelected in frozenSelected)
        {
            pointList.AppendLine($"{loopSelected.DbEntry.Latitude},{loopSelected.DbEntry.Longitude}");
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(pointList.ToString());

        StatusContext.ToastSuccess($"Points To Clipboard for {frozenSelected.Count} Points");
    }
}