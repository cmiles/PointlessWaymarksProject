using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListPostItem : PostListListItem, IMapElementListItem
{
    private protected MapElementListPostItem(PostContentActions itemActions, PostContent dbEntry) : base(itemActions,
        dbEntry)
    {
    }
    
    public required MapElementSettings ElementSettings { get; set; }
    public string ElementType { get; set; } = "post";
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListPostItem> CreateInstance(PostContentActions itemActions, PostContent dbEntry, MapElementSettings elementSettings)
    {
        var newContent = new MapElementListPostItem(itemActions, dbEntry)
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