﻿using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml.PostHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentHistoryView;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PostContentEditor;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using LogTools = PointlessWaymarks.CommonTools.LogTools;

namespace PointlessWaymarks.CmsWpfControls.PostList;

public partial class PostContentActions : ObservableObject, IContentActions<PostContent>
{
    [ObservableProperty] private RelayCommand<PostContent> _deleteCommand;
    [ObservableProperty] private RelayCommand<PostContent> _editCommand;
    [ObservableProperty] private RelayCommand<PostContent> _extractNewLinksCommand;
    [ObservableProperty] private RelayCommand<PostContent> _generateHtmlCommand;
    [ObservableProperty] private RelayCommand<PostContent> _linkCodeToClipboardCommand;
    [ObservableProperty] private RelayCommand<PostContent> _viewOnSiteCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private RelayCommand<PostContent> _viewHistoryCommand;

    public PostContentActions(StatusControlContext statusContext)
    {
        _statusContext = statusContext;
        _deleteCommand = StatusContext.RunBlockingTaskCommand<PostContent>(Delete);
        _editCommand = StatusContext.RunNonBlockingTaskCommand<PostContent>(Edit);
        _extractNewLinksCommand = StatusContext.RunBlockingTaskCommand<PostContent>(ExtractNewLinks);
        _generateHtmlCommand = StatusContext.RunBlockingTaskCommand<PostContent>(GenerateHtml);
        _linkCodeToClipboardCommand = StatusContext.RunBlockingTaskCommand<PostContent>(DefaultBracketCodeToClipboard);
        _viewOnSiteCommand = StatusContext.RunBlockingTaskCommand<PostContent>(ViewOnSite);
        _viewHistoryCommand = StatusContext.RunNonBlockingTaskCommand<PostContent>(ViewHistory);
    }

    public string DefaultBracketCode(PostContent? content)
    {
        if (content?.ContentId == null) return string.Empty;
        return @$"{BracketCodePosts.Create(content)}";
    }

    public async Task DefaultBracketCodeToClipboard(PostContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = @$"{BracketCodePosts.Create(content)}{Environment.NewLine}";

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public async Task Delete(PostContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        if (content.Id < 1)
        {
            StatusContext.ToastError($"Post {content.Title} - Entry is not saved - Skipping?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        await Db.DeletePostContent(content.ContentId, StatusContext.ProgressTracker());

        var possibleContentDirectory = settings.LocalSitePostContentDirectory(content, false);
        if (possibleContentDirectory.Exists)
        {
            StatusContext.Progress($"Deleting Generated Folder {possibleContentDirectory.FullName}");
            possibleContentDirectory.Delete(true);
        }
    }

    public async Task Edit(PostContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null) return;

        var context = await Db.Context();

        var refreshedData = context.PostContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null)
        {StatusContext.ToastError(
                $"{content.Title} is no longer active in the database? Can not edit - look for a historic version...");
            return;
        }

        var newContentWindow = await PostContentEditorWindow.CreateInstance(refreshedData);

        await newContentWindow.PositionWindowAndShowOnUiThread();
    }

    public async Task ExtractNewLinks(PostContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var context = await Db.Context();

        var refreshedData = context.PostContents.SingleOrDefault(x => x.ContentId == content.ContentId);

        if (refreshedData == null) return;

        await LinkExtraction.ExtractNewAndShowLinkContentEditors(
            $"{refreshedData.BodyContent} {refreshedData.UpdateNotes}", StatusContext.ProgressTracker());
    }

    public async Task GenerateHtml(PostContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        StatusContext.Progress($"Generating Html for {content.Title}");

        var htmlContext = new SinglePostPage(content);

        await htmlContext.WriteLocalHtml();

        StatusContext.ToastSuccess($"Generated {htmlContext.PageUrl}");
    }

    public async Task ViewOnSite(PostContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var settings = UserSettingsSingleton.CurrentSettings();

        var url = $@"{settings.PostPageUrl(content)}";

        var ps = new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    public async Task ViewHistory(PostContent? content)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (content == null)
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var db = await Db.Context();

        StatusContext.Progress($"Looking up Historic Entries for {content.Title}");

        var historicItems = await db.HistoricPostContents.Where(x => x.ContentId == content.ContentId).ToListAsync();

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

    public static async Task<PostListListItem> ListItemFromDbItem(PostContent content, PostContentActions itemActions,
        bool showType)
    {
        var item = await PostListListItem.CreateInstance(itemActions);
        item.DbEntry = content;
        item.SmallImageUrl = ContentListContext.GetSmallImageUrl(content);
        item.ShowType = showType;
        return item;
    }
}