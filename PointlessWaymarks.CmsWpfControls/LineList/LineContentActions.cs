using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.LineHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.ContentMap;
using PointlessWaymarks.CmsWpfControls.LineContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LineList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LineContentActions : IContentActions<LineContent>
{
    public LineContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        BuildCommands();
    }

    public StatusControlContext StatusContext { get; set; }

    public string DefaultBracketCode(LineContent content)
    {
        return $"{BracketCodeLines.Create(content)}";
    }

    [BlockingCommand]
    public async Task DefaultBracketCodeToClipboard(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = $"{BracketCodeLines.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    public async Task Delete(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError($"Line {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        await Db.DeleteLineContent(content.ContentId, StatusContext.ProgressTracker());

        var possibleContentDirectory = settings.LocalSiteLineContentDirectory(content, false);
        if (possibleContentDirectory.Exists)
        {
            StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
            possibleContentDirectory.Delete(true);
        }
    }

    [NonBlockingCommand]
    public async Task Edit(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.LineContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        var newContentWindow = await LineContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    public async Task ExtractNewLinks(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();

        var refreshedData = context.LineContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtml(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        var htmlContext = new SingleLinePage(content);

        await htmlContext.WriteLocalHtml();

        StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NonBlockingCommand]
    public async Task ViewHistory(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricLineContents.Where(x => x.ContentId == content.ContentId).ToListAsync();

        StatusContext.Progress($"Found {historicItems.Count} Historic Entries");

        if (historicItems.Count < 1)
        {
            StatusContext.ToastWarning("No History to Show...");
            return;
        }

        var historicView = new ContentViewHistoryPage($"Historic Entries - {content.Title}",
            UserSettingsSingleton.CurrentSettings().SiteName, $"Historic Entries - {content.Title}",
            historicItems.OrderByDescending(x => x.LastUpdatedOn.HasValue).ThenByDescending(x => x.LastUpdatedOn)
                .Select(LogTools.SafeObjectDump).ToList());

        historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task ViewOnSite(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = settings.LinePageUrl(content);

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    [BlockingCommand]
    public async Task ViewSitePreview(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = settings.LinePageUrl(content);

        await ThreadSwitcher.ResumeForegroundAsync();

        var sitePreviewWindow = await SiteOnDiskPreviewWindow.CreateInstance(url);

        await sitePreviewWindow.PositionWindowAndShowOnUiThread();
    }

    public static async Task<LineListListItem> ListItemFromDbItem(LineContent content, LineContentActions itemActions,
        bool showType)
    {
        var item = await LineListListItem.CreateInstance(itemActions);
        item.DbEntry = content;
        item.SmallImageUrl = ContentListContext.GetSmallImageUrl(content);
        item.ShowType = showType;
        return item;
    }

    [NonBlockingCommand]
    public async Task SearchRecordedDatesForPhotoContent(LineContent? lineContent)
    {
        if (lineContent == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        await PhotoContentActions.RunReport(async () => await SearchRecordedDatesForPhotoContentFilter(lineContent),
            $"Line {lineContent.Title ?? string.Empty} - {SearchRecordedDatesForPhotoContentDateRange(lineContent).start:M/d/yyyy hh:mm:ss tt} to {SearchRecordedDatesForPhotoContentDateRange(lineContent).end:M/d/yyyy hh:mm:ss tt}");
    }

    /// <summary>
    ///     Uses the recorded on values of the Line Content to create a date range to search - this will always return a
    ///     valid date range but if you pass in LineContent with null for both Recorded on values you will get a 'Now' date
    ///     range (which is valid, but doesn't make sense) - you should guard against that in calling code
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static (DateTime start, DateTime end) SearchRecordedDatesForPhotoContentDateRange(LineContent content)
    {
        var dateSearchStart = content.RecordingStartedOn?.Date ?? content.RecordingEndedOn?.Date ?? DateTime.Now.Date;
        var dateSearchEnd = content.RecordingEndedOn?.Date.AddDays(1) ??
                            content.RecordingStartedOn?.Date.AddDays(1) ?? DateTime.Now.Date.AddDays(1);

        return (dateSearchStart, dateSearchEnd);
    }

    public static (DateTime start, DateTime end) SearchRecordedDatesForPhotoContentDateRangeUtc(LineContent content)
    {
        var dateSearchStart = content.RecordingStartedOnUtc?.Date ??
                              content.RecordingEndedOnUtc?.Date ?? DateTime.Now.Date;
        var dateSearchEnd = content.RecordingEndedOnUtc?.Date.AddDays(1) ??
                            content.RecordingStartedOnUtc?.Date.AddDays(1) ?? DateTime.Now.Date.AddDays(1);

        return (dateSearchStart, dateSearchEnd);
    }

    public async Task<List<object>> SearchRecordedDatesForPhotoContentFilter(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return [];
        }

        if (content.RecordingStartedOn == null && content.RecordingEndedOn == null)
        {
            StatusContext.ToastError("Line doesn't have Recorded On dates to work with?");
            return [];
        }

        var dateSearchRange = SearchRecordedDatesForPhotoContentDateRangeUtc(content);

        var db = await Db.Context();

        return
            (await db.PhotoContents
                .Where(x =>
                    x.PhotoCreatedOnUtc != null
                        ? x.PhotoCreatedOnUtc >= dateSearchRange.start && x.PhotoCreatedOnUtc <= dateSearchRange.end
                        : x.PhotoCreatedOn >= dateSearchRange.start && x.PhotoCreatedOn <= dateSearchRange.end)
                .ToListAsync()).Cast<object>().ToList();
    }


    [NonBlockingCommand]
    public async Task ShowOnMap(LineContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError("Entry is not saved - Skipping?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var mapWindow =
            await ContentMapWindow.CreateInstance(new ContentMapListLoader("Mapped Content",
                [content.ContentId]));

        await mapWindow.PositionWindowAndShowOnUiThread();
    }
}