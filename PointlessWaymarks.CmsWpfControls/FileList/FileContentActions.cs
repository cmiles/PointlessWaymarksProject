using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.FileHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.FileContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.FileList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FileContentActions : IContentActions<FileContent>
{
    public FileContentActions(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        BuildCommands();
    }

    public string DefaultBracketCode(FileContent? content)
    {
        if (content?.ContentId == null) return string.Empty;
        return content.MainPicture != null
            ? @$"{BracketCodeFileImage.Create(content)}"
            : @$"{BracketCodeFiles.Create(content)}";
    }

    [BlockingCommand]
    public async Task DefaultBracketCodeToClipboard(FileContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = content.MainPicture != null
            ? @$"{BracketCodeFileImage.Create(content)}{Environment.NewLine}"
            : @$"{BracketCodeFiles.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    [BlockingCommand]
    public async Task Delete(FileContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError($"File {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        await Db.DeleteFileContent(content.ContentId, StatusContext.ProgressTracker());

        var possibleContentDirectory = settings.LocalSiteFileContentDirectory(content, false);
        if (possibleContentDirectory.Exists)
        {
            StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
            possibleContentDirectory.Delete(true);
        }
    }

    [NonBlockingCommand]
    public async Task Edit(FileContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.FileContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
        {
            StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");
            return;
        }

        var newContentWindow = await FileContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    public async Task ExtractNewLinks(FileContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();
        var refreshedData = context.FileContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
    }

    [BlockingCommand]
    public async Task GenerateHtml(FileContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        var htmlContext = new SingleFilePage(content);

        await htmlContext.WriteLocalHtml();

        StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
    }

    public StatusControlContext StatusContext { get; set; }

    [NonBlockingCommand]
    public async Task ViewHistory(FileContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricFileContents.Where(x => x.ContentId == content.ContentId).ToListAsync();

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
    public async Task ViewOnSite(FileContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.FilePageUrl(content)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    //TODO: Eliminate this with Metalama
    public event PropertyChangedEventHandler? PropertyChanged;

    public static async Task<FileListListItem> ListItemFromDbItem(FileContent content, FileContentActions itemActions,
        bool showType)
    {
        var item = await FileListListItem.CreateInstance(itemActions);
        item.DbEntry = content;
        item.SmallImageUrl = ContentListContext.GetSmallImageUrl(content);
        item.ShowType = showType;
        return item;
    }

    [NonBlockingCommand]
    public async Task ViewFile(FileContent? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            StatusContext.ToastError("Nothing Items to Open?");
            return;
        }

        if (string.IsNullOrWhiteSpace(listItem.OriginalFileName))
        {
            StatusContext.ToastError("No File?");
            return;
        }

        var toOpen = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentFile(listItem);

        if (toOpen is not { Exists: true })
        {
            StatusContext.ToastError("File doesn't exist?");
            return;
        }

        var url = toOpen.FullName;

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}