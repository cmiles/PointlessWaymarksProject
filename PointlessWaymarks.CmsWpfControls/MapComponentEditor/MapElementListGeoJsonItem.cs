using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListGeoJsonItem : GeoJsonListListItem, IMapElementListItem
{
    protected MapElementListGeoJsonItem(GeoJsonContentActions itemActions, GeoJsonContent dbEntry) : base(itemActions,
        dbEntry)
    {
    }
    
    public required MapElementSettings ElementSettings { get; set; }
    public string ElementType { get; set; } = "geojson";
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListGeoJsonItem> CreateInstance(GeoJsonContentActions itemActions, GeoJsonContent dbEntry, MapElementSettings elementSettings)
    {
        var newContent = new MapElementListGeoJsonItem(itemActions, dbEntry)
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