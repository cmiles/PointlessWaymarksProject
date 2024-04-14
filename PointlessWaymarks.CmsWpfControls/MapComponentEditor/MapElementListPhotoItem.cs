using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListPhotoItem : PhotoListListItem, IMapElementListItem
{
    private protected MapElementListPhotoItem(PhotoContentActions itemActions, PhotoContent dbEntry) : base(itemActions,
        dbEntry)
    {
    }
    
    public MapElementSettings ElementSettings { get; set; } = new();
    public string ElementType { get; set; } = "photo";
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListPhotoItem> CreateInstance(PhotoContentActions itemActions, PhotoContent dbEntry, MapElementSettings elementSettings)
    {
        var newContent = new MapElementListPhotoItem(itemActions, dbEntry)
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