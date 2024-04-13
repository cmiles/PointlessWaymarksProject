using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.VideoList;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListVideoItem : VideoListListItem, IMapElementListItem
{
    private protected MapElementListVideoItem(VideoContentActions itemActions, VideoContent dbEntry) : base(itemActions,
        dbEntry)
    {
    }

    public string ElementType { get; set; } = "video";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; }
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListVideoItem> CreateInstance(VideoContentActions itemActions)
    {
        return Task.FromResult(new MapElementListVideoItem(itemActions, VideoContent.CreateInstance()));
    }
}