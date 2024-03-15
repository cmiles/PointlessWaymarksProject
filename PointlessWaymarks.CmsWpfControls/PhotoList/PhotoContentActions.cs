using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.ContentMap;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PhotoList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class PhotoContentActions : IContentActions<PhotoContent>
{
    public PhotoContentActions(StatusControlContext? statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        BuildCommands();
    }

    public StatusControlContext StatusContext { get; set; }

    public string DefaultBracketCode(PhotoContent? content)
    {
        return content?.ContentId == null ? string.Empty : $"{BracketCodePhotos.Create(content)}";
    }

    [BlockingCommand]
    public async Task DefaultBracketCodeToClipboard(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = $"{BracketCodePhotos.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [NonBlockingCommand]
    public async Task Delete(PhotoContent? content)
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

        var settings = UserSettingsSingleton.CurrentSettings();

        await Db.DeletePhotoContent(content.ContentId, StatusContext.ProgressTracker());

        var possibleContentDirectory = settings.LocalSitePhotoContentDirectory(content, false);

        if (possibleContentDirectory.Exists)
        {
            StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
            possibleContentDirectory.Delete(true);
        }
    }

    [NonBlockingCommand]
    public async Task Edit(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.PhotoContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        var newContentWindow = await PhotoContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    public async Task ExtractNewLinks(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();
        var refreshedData = context.PhotoContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtml(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        var htmlContext = new SinglePhotoPage(content);

        await htmlContext.WriteLocalHtml();

        StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NonBlockingCommand]
    public async Task ViewHistory(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricPhotoContents.Where(x => x.ContentId == content.ContentId).ToListAsync();

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
    public async Task ViewOnSite(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = settings.PhotoPageUrl(content);

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    [BlockingCommand]
    public async Task ViewSitePreview(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = settings.PhotoPageUrl(content);

        await ThreadSwitcher.ResumeForegroundAsync();

        var sitePreviewWindow = await SiteOnDiskPreviewWindow.CreateInstance(url);

        await sitePreviewWindow.PositionWindowAndShowOnUiThread();
    }

    private static async Task<List<object>> ApertureFilter(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        if (content == null) return [];

        return (await db.PhotoContents.Where(x => x.Aperture == content.Aperture).ToListAsync()).Cast<object>()
            .ToList();
    }

    [NonBlockingCommand]
    public async Task ApertureSearch(PhotoContent? content)
    {
        await RunReport(async () => await ApertureFilter(content), $"Aperture - {content?.Aperture}");
    }

    public static async Task<List<object>> CameraMakeFilter(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        if (content == null) return [];

        return (await db.PhotoContents.Where(x => x.CameraMake == content.CameraMake).ToListAsync()).Cast<object>()
            .ToList();
    }


    [NonBlockingCommand]
    public async Task CameraMakeSearch(PhotoContent? content)
    {
        await RunReport(async () => await CameraMakeFilter(content), $"Camera Make - {content?.CameraMake}");
    }

    public static async Task<List<object>> CameraModelFilter(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        if (content == null) return [];

        return (await db.PhotoContents.Where(x => x.CameraModel == content.CameraModel).ToListAsync()).Cast<object>()
            .ToList();
    }


    [NonBlockingCommand]
    public async Task CameraModelSearch(PhotoContent? content)
    {
        await RunReport(async () => await CameraModelFilter(content), $"Camera Model - {content?.CameraModel}");
    }

    public static async Task<List<object>> FocalLengthFilter(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        if (content == null) return [];

        return (await db.PhotoContents.Where(x => x.FocalLength == content.FocalLength).ToListAsync()).Cast<object>()
            .ToList();
    }


    [NonBlockingCommand]
    public async Task FocalLengthSearch(PhotoContent? content)
    {
        await RunReport(async () => await FocalLengthFilter(content), $"Focal Length - {content?.FocalLength}");
    }

    public static async Task<List<object>> IsoFilter(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        if (content == null) return [];

        return (await db.PhotoContents.Where(x => x.Iso == content.Iso).ToListAsync()).Cast<object>().ToList();
    }

    [NonBlockingCommand]
    public async Task IsoSearch(PhotoContent? content)
    {
        await RunReport(async () => await IsoFilter(content), $"ISO - {content?.Iso}");
    }

    public static async Task<List<object>> LensFilter(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        if (content == null) return [];

        return (await db.PhotoContents.Where(x => x.Lens == content.Lens).ToListAsync()).Cast<object>().ToList();
    }

    [NonBlockingCommand]
    public async Task LensSearch(PhotoContent? content)
    {
        await RunReport(async () => await LensFilter(content), $"Lens - {content?.Lens}");
    }

    public static async Task<PhotoListListItem> ListItemFromDbItem(PhotoContent content,
        PhotoContentActions photoContentActions,
        bool showType)
    {
        var item = await PhotoListListItem.CreateInstance(photoContentActions);
        item.DbEntry = content;
        var (smallImageUrl, displayImageUrl) = ContentListContext.GetContentItemImageUrls(content);
        item.SmallImageUrl = smallImageUrl;
        item.DisplayImageUrl = displayImageUrl;
        item.ShowType = showType;
        return item;
    }


    public static async Task<List<object>> PhotoTakenOnFilter(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        if (content == null) return [];

        //Todo: I think this should be possible via something like DbFunctions or EF functions?
        //I didn't understand what approach to take from a few google searches...
        var dateTimeAfter = content.PhotoCreatedOn.Date;
        var dateTimeBefore = content.PhotoCreatedOn.Date.AddDays(1);

        return (await db.PhotoContents
                .Where(x => x.PhotoCreatedOn >= dateTimeAfter && x.PhotoCreatedOn < dateTimeBefore).ToListAsync())
            .Cast<object>().ToList();
    }


    [NonBlockingCommand]
    public async Task PhotoTakenOnSearch(PhotoContent? content)
    {
        await RunReport(async () => await PhotoTakenOnFilter(content),
            $"Photo Created On - {content?.PhotoCreatedOn.Date:D}");
    }

    public static async Task RunReport(Func<Task<List<object>>> toRun, string title)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var reportLoader = new ContentListLoaderReport(toRun, PhotoListLoader.SortContextPhotoDefault());

        var newWindow =
            await PhotoListWindow.CreateInstance(
                await PhotoListWithActionsContext.CreateInstance(null, null, reportLoader));
        await newWindow.PositionWindowAndShowOnUiThread();
        newWindow.WindowTitle = title;
    }

    [NonBlockingCommand]
    public async Task ShowOnMap(PhotoContent? content)
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

        if (content.Latitude == null || content.Longitude == null)
        {
            StatusContext.ToastError("No Location Data?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var mapWindow =
            await ContentMapWindow.CreateInstance(new ContentMapListLoader("Mapped Content",
                [content.ContentId]));

        await mapWindow.PositionWindowAndShowOnUiThread();
    }

    public static async Task<List<object>> ShutterSpeedSearch(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return [];

        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.ShutterSpeed == content.ShutterSpeed).ToListAsync()).Cast<object>()
            .ToList();
    }

    [NonBlockingCommand]
    public async Task ShutterSpeedSearchSearch(PhotoContent? content)
    {
        await RunReport(async () => await ShutterSpeedSearch(content), $"Shutter Speed - {content?.ShutterSpeed}");
    }

    [NonBlockingCommand]
    public async Task ViewFile(PhotoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        try
        {
            var context = await Db.Context();

            var refreshedData = context.PhotoContents.SingleOrDefault(x => x.ContentId == content.ContentId);

            var possibleFile = UserSettingsSingleton.CurrentSettings().LocalMediaArchivePhotoContentFile(refreshedData);

            if (possibleFile is not { Exists: true })
            {
                StatusContext.ToastWarning("No Media File Found?");
                return;
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            var ps = new ProcessStartInfo(possibleFile.FullName) { UseShellExecute = true, Verb = "open" };
            Process.Start(ps);
        }
        catch (Exception e)
        {
            StatusContext.ToastWarning($"Trouble Showing Image - {e.Message}");
        }
    }
}