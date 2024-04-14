using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListPointItem : PointListListItem, IMapElementListItem
{
    private protected MapElementListPointItem(PointContentActions itemActions, PointContentDto dbEntry) : base(itemActions,
        dbEntry)
    {
    }
    
    public MapElementSettings ElementSettings { get; set; } = new();
    public string ElementType { get; set; } = "point";
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListPointItem> CreateInstance(PointContentActions itemActions, PointContentDto dbEntry, MapElementSettings elementSettings)
    {
        var newContent = new MapElementListPointItem(itemActions, dbEntry)
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