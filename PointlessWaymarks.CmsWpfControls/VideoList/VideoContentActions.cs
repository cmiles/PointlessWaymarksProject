using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.VideoHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CmsWpfControls.VideoContentEditor;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.VideoList;

public partial class VideoContentActions : ObservableObject, IContentActions<VideoContent>
{
    [ObservableProperty] private RelayCommand<VideoContent> _deleteCommand;
    [ObservableProperty] private RelayCommand<VideoContent> _editCommand;
    [ObservableProperty] private RelayCommand<VideoContent> _extractNewLinksCommand;
    [ObservableProperty] private RelayCommand<VideoContent> _generateHtmlCommand;
    [ObservableProperty] private RelayCommand<VideoContent> _linkCodeToClipboardCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand<VideoContent> _viewFileCommand;
    [ObservableProperty] private RelayCommand<VideoContent> _viewHistoryCommand;
    [ObservableProperty] private RelayCommand<VideoContent> _viewOnSiteCommand;

    public VideoContentActions(StatusControlContext statusContext)
    {
        _statusContext = statusContext;
        _deleteCommand = StatusContext.RunBlockingTaskCommand<VideoContent>(Delete);
        _editCommand = StatusContext.RunNonBlockingTaskCommand<VideoContent>(Edit);
        _extractNewLinksCommand = StatusContext.RunBlockingTaskCommand<VideoContent>(ExtractNewLinks);
        _generateHtmlCommand = StatusContext.RunBlockingTaskCommand<VideoContent>(GenerateHtml);
        _linkCodeToClipboardCommand = StatusContext.RunBlockingTaskCommand<VideoContent>(DefaultBracketCodeToClipboard);
        _viewOnSiteCommand = StatusContext.RunBlockingTaskCommand<VideoContent>(ViewOnSite);
        _viewFileCommand = StatusContext.RunNonBlockingTaskCommand<VideoContent>(ViewFile);
        _viewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<VideoContent>(ViewHistory);
    }

    public string DefaultBracketCode(VideoContent? content)
    {
        if (content?.ContentId == null) return string.Empty;
        return content.MainPicture != null
            ? @$"{BracketCodeVideoImage.Create(content)}"
            : @$"{BracketCodeVideoEmbed.Create(content)}";
    }

    public async Task DefaultBracketCodeToClipboard(VideoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = content.MainPicture != null
            ? @$"{BracketCodeVideoImage.Create(content)}{Environment.NewLine}"
            : @$"{BracketCodeVideoEmbed.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public async Task Delete(VideoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError($"Video {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        await Db.DeleteVideoContent(content.ContentId, StatusContext.ProgressTracker());

        var possibleContentDirectory = settings.LocalSiteVideoContentDirectory(content, false);
        if (possibleContentDirectory.Exists)
        {
            StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
            possibleContentDirectory.Delete(true);
        }
    }

    public async Task Edit(VideoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.VideoContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        var newContentWindow = await VideoContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public async Task ExtractNewLinks(VideoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();
        var refreshedData = context.VideoContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
    }

    public async Task GenerateHtml(VideoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        var htmlContext = new SingleVideoPage(content);

        await htmlContext.WriteLocalHtml();

        StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
    }

    public async Task ViewHistory(VideoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricVideoContents.Where(x => x.ContentId == content.ContentId).ToListAsync();

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

    public async Task ViewOnSite(VideoContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.VideoPageUrl(content)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    public static async Task<VideoListListItem> ListItemFromDbItem(VideoContent content,
        VideoContentActions itemActions,
        bool showType)
    {
        var item = await VideoListListItem.CreateInstance(itemActions);
        item.DbEntry = content;
        item.SmallImageUrl = ContentListContext.GetSmallImageUrl(content);
        item.ShowType = showType;

        return item;
    }

    public async Task ViewFile(VideoContent? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            StatusContext.ToastError("Nothing Items to Open?");
            return;
        }

        if (string.IsNullOrWhiteSpace(listItem.OriginalFileName))
        {
            StatusContext.ToastError("No Video?");
            return;
        }

        var toOpen = UserSettingsSingleton.CurrentSettings().LocalSiteVideoContentFile(listItem);

        if (toOpen is not { Exists: true })
        {
            StatusContext.ToastError("Video doesn't exist?");
            return;
        }

        var url = toOpen.FullName;

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}