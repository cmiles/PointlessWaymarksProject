using PointlessWaymarks.CmsData.Database.Models;
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

    public string ElementType { get; set; } = "photo";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; }
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListPhotoItem> CreateInstance(PhotoContentActions itemActions)
    {
        return Task.FromResult(new MapElementListPhotoItem(itemActions, PhotoContent.CreateInstance()));
    }
}