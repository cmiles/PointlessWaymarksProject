using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.VideoList;

[NotifyPropertyChanged]
public partial class VideoListListItem : IContentListItem, IContentListImage
{
    protected VideoListListItem(VideoContentActions itemActions, VideoContent dbEntry)
    {
        DbEntry = dbEntry;
        ItemActions = itemActions;
    }

    public VideoContent DbEntry { get; set; }
    public VideoContentActions ItemActions { get; set; }
    public bool ShowType { get; set; }
    public string? DisplayImageUrl { get; set; }
    public string? SmallImageUrl { get; set; }

    public IContentCommon Content()
    {
        return DbEntry;
    }

    public Guid? ContentId()
    {
        return DbEntry.ContentId;
    }

    public string DefaultBracketCode()
    {
        return ItemActions.DefaultBracketCode(DbEntry);
    }

    public async Task DefaultBracketCodeToClipboard()
    {
        await ItemActions.DefaultBracketCodeToClipboard(DbEntry);
    }

    public async Task Delete()
    {
        await ItemActions.Delete(DbEntry);
    }

    public async Task Edit()
    {
        await ItemActions.Edit(DbEntry);
    }

    public async Task ExtractNewLinks()
    {
        await ItemActions.ExtractNewLinks(DbEntry);
    }

    public async Task GenerateHtml()
    {
        await ItemActions.GenerateHtml(DbEntry);
    }

    public async Task ViewHistory()
    {
        await ItemActions.ViewHistory(DbEntry);
    }

    public async Task ViewOnSite()
    {
        await ItemActions.ViewOnSite(DbEntry);
    }

    public CurrentSelectedTextTracker? SelectedTextTracker { get; set; } = new();

    public static Task<VideoListListItem> CreateInstance(VideoContentActions itemActions)
    {
        return Task.FromResult(new VideoListListItem(itemActions, VideoContent.CreateInstance()));
    }
}