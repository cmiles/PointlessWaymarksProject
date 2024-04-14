using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
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
    
    public MapElementSettings ElementSettings { get; set; }
    public string ElementType { get; set; } = "video";
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListVideoItem> CreateInstance(VideoContentActions itemActions, VideoContent dbEntry, MapElementSettings elementSettings)
    {
        var newContent = new MapElementListVideoItem(itemActions, dbEntry)
        {
            DbEntry = dbEntry,
            ElementSettings = elementSettings,
            Title = dbEntry.Title ?? string.Empty
        };
        
        var (smallImageUrl, displayImageUrl) = ContentListContext.GetContentItemImageUrls(dbEntry);
        newContent.SmallImageUrl = smallImageUrl;
        newContent.DisplayImageUrl = displayImageUrl;
        
        return Task.FromResult(newContent);
    }
}