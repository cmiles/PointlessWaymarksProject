using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListFileItem : FileListListItem, IMapElementListItem
{
    private protected MapElementListFileItem(FileContentActions itemActions, FileContent dbEntry) : base(itemActions, dbEntry)
    {
    }
    
    public MapElementSettings ElementSettings { get; set; } = new();
    public string ElementType { get; set; } = "file";
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListFileItem> CreateInstance(FileContentActions itemActions, FileContent dbEntry, MapElementSettings elementSettings)
    {
        var newContent = new MapElementListFileItem(itemActions, dbEntry)
        {
            DbEntry = dbEntry,
            ElementSettings =elementSettings,
            Title = dbEntry.Title ?? string.Empty
        };
        
        var (smallImageUrl, displayImageUrl) = ContentListContext.GetContentItemImageUrls(dbEntry);
        newContent.SmallImageUrl = smallImageUrl;
        newContent.DisplayImageUrl = displayImageUrl;

        return Task.FromResult(newContent);
    }
}