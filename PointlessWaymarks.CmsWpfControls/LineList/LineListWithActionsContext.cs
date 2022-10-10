using System.IO;
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

namespace PointlessWaymarks.CmsWpfControls.LineList;

[ObservableObject]
public partial class LineListWithActionsContext
{
    [ObservableProperty] private RelayCommand _addIntersectionTagsToSelectedCommand;
    [ObservableProperty] private RelayCommand _lineLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private RelayCommand _lineStatsCodesToClipboardForSelectedCommand;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private RelayCommand _refreshDataCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public LineListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
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

        var toProcess = new List<(LineContent dbClone, IFeature lineFeature)>();

        foreach (var loopSelected in frozenSelect)
            toProcess.Add(((LineContent)new LineContent().InjectFrom(loopSelected.DbEntry),
                loopSelected.DbEntry.FeatureFromGeoJsonLine()));

        var tagReturn = tagger.Tags(settingsFileInfo.FullName, toProcess.Select(x => x.lineFeature).ToList(),
            StatusContext.ProgressTracker());

        var updateTime = DateTime.Now;

        foreach (var loopSelected in toProcess)
        {
            processedCount++;

            try
            {
                var taggerResult = tagReturn.Single(x => x.Feature == loopSelected.lineFeature);

                if (!taggerResult.Tags.Any())
                {
                    noTagsList.Add($"{loopSelected.dbClone.Title} - no tags found");
                    StatusContext.Progress(
                        $"Processed - {loopSelected.dbClone.Title} - no tags found - Line {processedCount} of {frozenSelect.Count}");
                    continue;
                }

                var tagListForIntersection = Db.TagListParse(loopSelected.dbClone.Tags);
                tagListForIntersection.AddRange(taggerResult.Tags);
                loopSelected.dbClone.Tags = Db.TagListJoin(tagListForIntersection);
                loopSelected.dbClone.LastUpdatedBy = "Feature Intersection Tagger";
                loopSelected.dbClone.LastUpdatedOn = updateTime;

                var (saveGenerationReturn, _) =
                    await LineGenerator.SaveAndGenerateHtml(loopSelected.dbClone, DateTime.Now,
                        StatusContext.ProgressTracker());

                if (saveGenerationReturn.HasError)
                    //TODO: Need alerting on this that would actually be seen...
                {
                    Log.ForContext("generationError", saveGenerationReturn.GenerationNote)
                        .ForContext("generationException", saveGenerationReturn.Exception?.ToString() ?? string.Empty)
                        .Error(
                            "Line Save Error during Selected Line Feature Intersection Tagging");
                    errorList.Add(
                        $"Save Failed! Line: {loopSelected.dbClone.Title}, {saveGenerationReturn.GenerationNote}");
                    continue;
                }

                successList.Add(
                    $"{loopSelected.dbClone.Title} - found Tags {string.Join(", ", taggerResult.Tags)}");
                StatusContext.Progress(
                    $"Processed - {loopSelected.dbClone.Title} - found Tags {string.Join(", ", taggerResult.Tags)} - Line {processedCount} of {frozenSelect.Count}");
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"Line Save Error during Selected Line Feature Intersection Tagging {loopSelected.dbClone.Title}, {loopSelected.dbClone.ContentId}");
                errorList.Add(
                    $"Save Failed! Line: {loopSelected.dbClone.Title}, {e.Message}");
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
                current + @$"{BracketCodeLineLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new LineListLoader(100), WindowStatus);

        LineLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(LinkBracketCodesToClipboardForSelected);
        LineStatsCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(StatsBracketCodesToClipboardForSelected);
        AddIntersectionTagsToSelectedCommand =
            StatusContext.RunBlockingTaskWithCancellationCommand(AddIntersectionTagsToSelected,
                "Cancel Feature Intersection Tag Add");
        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);

        ListContext.ContextMenuItems = new List<ContextMenuItemData>
        {
            new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new()
            {
                ItemName = "Map Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new() { ItemName = "Text Code to Clipboard", ItemCommand = LineLinkCodesToClipboardForSelectedCommand },
            new() { ItemName = "Stats Code to Clipboard", ItemCommand = LineStatsCodesToClipboardForSelectedCommand },
            new() { ItemName = "Add Intersection Tags", ItemCommand = AddIntersectionTagsToSelectedCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    public List<LineListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is LineListListItem).Cast<LineListListItem>()
            .ToList() ?? new List<LineListListItem>();
    }

    private async Task StatsBracketCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = SelectedItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + @$"{BracketCodeLineStats.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }
}