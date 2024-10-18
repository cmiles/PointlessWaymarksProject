using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListImageItem : ImageListListItem, IMapElementListItem
{
    private protected MapElementListImageItem(ImageContentActions itemActions, ImageContent dbEntry) : base(itemActions,
        dbEntry)
    {
    }
    
    public required MapElementSettings ElementSettings { get; set; }
    public string ElementType { get; set; } = "image";
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListImageItem> CreateInstance(ImageContentActions itemActions, ImageContent dbEntry, MapElementSettings elementSettings)
    {
        var newContent = new MapElementListImageItem(itemActions, dbEntry)
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