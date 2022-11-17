using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.ImageHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.ImageContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.ImageList;

public partial class ImageContentActions : ObservableObject, IContentActions<ImageContent>
{
    [ObservableProperty] private RelayCommand<ImageContent> _deleteCommand;
    [ObservableProperty] private RelayCommand<ImageContent> _editCommand;
    [ObservableProperty] private RelayCommand<ImageContent> _extractNewLinksCommand;
    [ObservableProperty] private RelayCommand<ImageContent> _generateHtmlCommand;
    [ObservableProperty] private RelayCommand<ImageContent> _linkCodeToClipboardCommand;
    [ObservableProperty] private RelayCommand<ImageContent> _viewOnSiteCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand<ImageContent> _viewFileCommand;
    [ObservableProperty] private RelayCommand<ImageContent> _viewHistoryCommand;

    public ImageContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        DeleteCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(Delete);
        EditCommand = StatusContext.RunNonBlockingTaskCommand<ImageContent>(Edit);
        ExtractNewLinksCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(ExtractNewLinks);
        GenerateHtmlCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(GenerateHtml);
        LinkCodeToClipboardCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(DefaultBracketCodeToClipboard);
        ViewOnSiteCommand = StatusContext.RunBlockingTaskCommand<ImageContent>(ViewOnSite);
        ViewFileCommand = StatusContext.RunNonBlockingTaskCommand<ImageContent>(ViewFile);
        ViewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<ImageContent>(ViewHistory);
    }

    public string DefaultBracketCode(ImageContent content)
    {
        return content?.ContentId == null ? string.Empty : @$"{BracketCodeImages.Create(content)}";
    }

    public async Task DefaultBracketCodeToClipboard(ImageContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = @$"{BracketCodeImages.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public async Task Delete(ImageContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError($"Image {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        await Db.DeleteImageContent(content.ContentId, StatusContext.ProgressTracker());

        var possibleContentDirectory = settings.LocalSiteImageContentDirectory(content, false);
        if (possibleContentDirectory.Exists)
        {
            StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
            possibleContentDirectory.Delete(true);
        }
    }

    public async Task Edit(ImageContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.ImageContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");

        await ThreadSwitcher.ResumeForegroundAsync();

        var newContentWindow = await ImageContentEditorWindow.CreateInstance(refreshedData);

        newContentWindow.PositionWindowAndShow();

        await ThreadSwitcher.ResumeBackgroundAsync();
    }

    public async Task ExtractNewLinks(ImageContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();
        var refreshedData = context.ImageContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
    }

    public async Task GenerateHtml(ImageContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        var htmlContext = new SingleImagePage(content);

        await htmlContext.WriteLocalHtml();

        StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
    }

    public async Task ViewOnSite(ImageContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.ImagePageUrl(content)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    public async Task ViewHistory(ImageContent content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricImageContents.Where(x => x.ContentId == content.ContentId).ToListAsync();

        StatusContext.Progress($"Found {historicItems.Count} Historic Entries");

        if (historicItems.Count < 1)
        {
            StatusContext.ToastWarning("No History to Show...");
            return;
        }

        var historicView = new ContentViewHistoryPage($"Historic Entries - {content.Title}",
            UserSettingsSingleton.CurrentSettings().SiteName, $"Historic Entries - {content.Title}",
            historicItems.OrderByDescending(x => x.LastUpdatedOn.HasValue).ThenByDescending(x => x.LastUpdatedOn)
                .Select(PointlessWaymarksLogTools.SafeObjectDump).ToList());

        historicView.WriteHtmlToTempFolderAndShow(StatusContext.ProgressTracker());
    }

    public static ImageListListItem ListItemFromDbItem(ImageContent content, ImageContentActions itemActions,
        bool showType)
    {
        return new()
        {
            DbEntry = content,
            SmallImageUrl = ContentListContext.GetSmallImageUrl(content),
            ItemActions = itemActions,
            ShowType = showType
        };
    }

    public async Task ViewFile(ImageContent listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            StatusContext.ToastError("Nothing Items to Open?");
            return;
        }

        if (string.IsNullOrWhiteSpace(listItem.OriginalFileName))
        {
            StatusContext.ToastError("No Image?");
            return;
        }

        var toOpen = UserSettingsSingleton.CurrentSettings().LocalSiteImageContentFile(listItem);

        if (toOpen is not { Exists: true })
        {
            StatusContext.ToastError("Image doesn't exist?");
            return;
        }

        var url = toOpen.FullName;

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}