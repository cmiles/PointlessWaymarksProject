using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PhotoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PhotoList;

public partial class PhotoContentActions : ObservableObject, IContentActions<PhotoContent>
{
    [ObservableProperty] private RelayCommand<PhotoContent> _apertureSearchCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _cameraMakeSearchCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _cameraModelSearchCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _deleteCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _editCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _extractNewLinksCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _focalLengthSearchCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _generateHtmlCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _isoSearchCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _lensSearchCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _linkCodeToClipboardCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _viewOnSiteCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _photoTakenOnSearchCommand;
    [ObservableProperty] private RelayCommand<PhotoContent> _shutterSpeedSearchCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand<PhotoContent> _viewFileCommand;

    public PhotoContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        DeleteCommand = StatusContext.RunBlockingTaskCommand<PhotoContent>(Delete);
        EditCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(Edit);
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<PhotoContent>(ExtractNewLinks);
        GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<PhotoContent>(GenerateHtml);
        LinkCodeToClipboardCommand = StatusContext.RunBlockingTaskCommand<PhotoContent>(DefaultBracketCodeToClipboard);
        ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand<PhotoContent>(ViewOnSite);
        ViewFileCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(ViewFile);
        ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(ViewHistory);

        ApertureSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
            await RunReport(async () => await ApertureSearch(x), $"Aperture - {x.Aperture}"));
        CameraMakeSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
            await RunReport(async () => await CameraMakeSearch(x), $"Camera Make - {x.CameraMake}"));
        CameraModelSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
            await RunReport(async () => await CameraModelSearch(x), $"Camera Model - {x.CameraModel}"));
        FocalLengthSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
            await RunReport(async () => await FocalLengthSearch(x), $"Focal Length - {x.FocalLength}"));
        IsoSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
            await RunReport(async () => await IsoSearch(x), $"ISO - {x.Iso}"));
        LensSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
            await RunReport(async () => await LensSearch(x), $"Lens - {x.Lens}"));
        PhotoTakenOnSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
            await RunReport(async () => await PhotoTakenOnSearch(x), $"Photo Created On - {x.PhotoCreatedOn.Date:D}"));
        ShutterSpeedSearchCommand = StatusContext.RunNonBlockingTaskCommand<PhotoContent>(async x =>
            await RunReport(async () => await ShutterSpeedSearch(x), $"Shutter Speed - {x.ShutterSpeed}"));
    }

    public string DefaultBracketCode(PhotoContent content)
    {
        return content?.ContentId == null ? string.Empty : @$"{BracketCodePhotos.Create(content)}";
    }

    public async Task DefaultBracketCodeToClipboard(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = @$"{BracketCodePhotos.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public async Task Delete(PhotoContent content)
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

    public async Task Edit(PhotoContent content)
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

    public async Task ExtractNewLinks(PhotoContent content)
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

    public async Task GenerateHtml(PhotoContent content)
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

    public async Task ViewOnSite(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.PhotoPageUrl(content)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    public async Task ViewHistory(PhotoContent content)
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
                .Select(LogHelpers.SafeObjectDump).ToList());

        historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
    }

    public RelayCommand<PhotoContent> ViewHistoryCommand { get; set; }

    private static async Task<List<object>> ApertureSearch(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.Aperture == content.Aperture).ToListAsync()).Cast<object>()
            .ToList();
    }

    public static async Task<List<object>> CameraMakeSearch(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.CameraMake == content.CameraMake).ToListAsync()).Cast<object>()
            .ToList();
    }

    public static async Task<List<object>> CameraModelSearch(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.CameraModel == content.CameraModel).ToListAsync()).Cast<object>()
            .ToList();
    }

    public static async Task<List<object>> FocalLengthSearch(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.FocalLength == content.FocalLength).ToListAsync()).Cast<object>()
            .ToList();
    }

    public static async Task<List<object>> IsoSearch(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.Iso == content.Iso).ToListAsync()).Cast<object>().ToList();
    }

    public static async Task<List<object>> LensSearch(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.Lens == content.Lens).ToListAsync()).Cast<object>().ToList();
    }

    public static PhotoListListItem ListItemFromDbItem(PhotoContent content, PhotoContentActions photoContentActions,
        bool showType)
    {
        return new PhotoListListItem
        {
            DbEntry = content,
            SmallImageUrl = ContentListContext.GetSmallImageUrl(content),
            ItemActions = photoContentActions,
            ShowType = showType
        };
    }


    public static async Task<List<object>> PhotoTakenOnSearch(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        //Todo: I think this should be possible via something like DbFunctions or EF functions?
        //I didn't understand what approach to take from a few google searches...
        var dateTimeAfter = content.PhotoCreatedOn.Date;
        var dateTimeBefore = content.PhotoCreatedOn.Date.AddDays(1);

        return (await db.PhotoContents
                .Where(x => x.PhotoCreatedOn >= dateTimeAfter && x.PhotoCreatedOn < dateTimeBefore).ToListAsync())
            .Cast<object>().ToList();
    }

    public static async Task RunReport(Func<Task<List<object>>> toRun, string title)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var reportLoader = new ContentListLoaderReport(toRun, PhotoListLoader.SortContextPhotoDefault());

        var newWindow = await PhotoListWindow.CreateInstance(new PhotoListWithActionsContext(null, reportLoader));
        await newWindow.PositionWindowAndShowOnUiThread();
    }

    public static async Task<List<object>> ShutterSpeedSearch(PhotoContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        return (await db.PhotoContents.Where(x => x.ShutterSpeed == content.ShutterSpeed).ToListAsync()).Cast<object>()
            .ToList();
    }

    public async Task ViewFile(PhotoContent content)
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