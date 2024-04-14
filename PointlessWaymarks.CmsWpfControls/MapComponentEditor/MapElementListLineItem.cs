using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListLineItem : LineListListItem, IMapElementListItem
{
    private protected MapElementListLineItem(LineContentActions itemActions, LineContent dbEntry) : base(itemActions,
        dbEntry)
    {
    }
    
    public MapElementSettings ElementSettings { get; set; } = new();
    public string ElementType { get; set; } = "line";
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListLineItem> CreateInstance(LineContentActions itemActions, LineContent dbEntry, MapElementSettings elementSettings)
    {
        var newContent = new MapElementListLineItem(itemActions, dbEntry)
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