﻿using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetTopologySuite.Features;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonList;

[ObservableObject]
public partial class GeoJsonListWithActionsContext
{
    [ObservableProperty] private RelayCommand _addIntersectionTagsToSelectedCommand;
    [ObservableProperty] private RelayCommand _geoJsonLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private RelayCommand _refreshDataCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus _windowStatus;


    public GeoJsonListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    private async Task AddIntersectionTagsToSelected(CancellationToken cancellationToken)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var frozenSelect = SelectedItems();

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

        var toProcess = new List<(GeoJsonContent dbClone, List<IFeature> geoJsonFeature)>();

        foreach (var loopSelected in frozenSelect)
            toProcess.Add(((GeoJsonContent)new GeoJsonContent().InjectFrom(loopSelected.DbEntry),
                loopSelected.DbEntry.FeaturesFromGeoJson()));

        var tagReturn = tagger.Tags(settingsFileInfo.FullName, toProcess.SelectMany(x => x.geoJsonFeature).ToList(),
            cancellationToken, StatusContext.ProgressTracker());

        var updateTime = DateTime.Now;

        var groupedToProcess = toProcess.GroupBy(x => x.dbClone)
            .Select(x => new { dbClone = x.Key, features = x.SelectMany(y => y.geoJsonFeature).ToList() }).ToList();

        foreach (var loopSelected in groupedToProcess)
        {
            processedCount++;

            try
            {
                var taggerResult = tagReturn.Where(x => loopSelected.features.Contains(x.Feature)).ToList();

                var taggerResultTags = taggerResult.SelectMany(x => x.Tags).ToList();

                if (!taggerResultTags.Any())
                {
                    noTagsList.Add($"{loopSelected.dbClone.Title} - no tags found");
                    StatusContext.Progress(
                        $"Processed - {loopSelected.dbClone.Title} - no tags found - GeoJson {processedCount} of {frozenSelect.Count}");
                    continue;
                }

                var tagListForIntersection = Db.TagListParse(loopSelected.dbClone.Tags);
                tagListForIntersection.AddRange(taggerResultTags);
                loopSelected.dbClone.Tags = Db.TagListJoin(tagListForIntersection);
                loopSelected.dbClone.LastUpdatedBy = "Feature Intersection Tagger";
                loopSelected.dbClone.LastUpdatedOn = updateTime;

                var (saveGenerationReturn, _) =
                    await GeoJsonGenerator.SaveAndGenerateHtml(loopSelected.dbClone, DateTime.Now,
                        StatusContext.ProgressTracker());

                if (saveGenerationReturn.HasError)
                    //TODO: Need alerting on this that would actually be seen...
                {
                    Log.ForContext("generationError", saveGenerationReturn.GenerationNote)
                        .ForContext("generationException", saveGenerationReturn.Exception?.ToString() ?? string.Empty)
                        .Error(
                            "GeoJson Save Error during Selected GeoJson Feature Intersection Tagging");
                    errorList.Add(
                        $"Save Failed! GeoJson: {loopSelected.dbClone.Title}, {saveGenerationReturn.GenerationNote}");
                    continue;
                }

                successList.Add(
                    $"{loopSelected.dbClone.Title} - found Tags {string.Join(", ", taggerResultTags)}");
                StatusContext.Progress(
                    $"Processed - {loopSelected.dbClone.Title} - found Tags {string.Join(", ", taggerResultTags)} - GeoJson {processedCount} of {frozenSelect.Count}");
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"GeoJson Save Error during Selected GeoJson Feature Intersection Tagging {loopSelected.dbClone.Title}, {loopSelected.dbClone.ContentId}");
                errorList.Add(
                    $"Save Failed! GeoJson: {loopSelected.dbClone.Title}, {e.Message}");
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

    private async Task LinkBracketCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = SelectedItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + @$"{BracketCodeGeoJsonLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new GeoJsonListLoader(100), WindowStatus);

        GeoJsonLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(LinkBracketCodesToClipboardForSelected);
        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);

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
                ItemName = "Text Code to Clipboard", ItemCommand = GeoJsonLinkCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Add Intersection Tags", ItemCommand = AddIntersectionTagsToSelectedCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    public List<GeoJsonListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is GeoJsonListListItem)
            .Cast<GeoJsonListListItem>().ToList() ?? new List<GeoJsonListListItem>();
    }
}