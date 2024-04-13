using PointlessWaymarks.CmsData.Database.Models;
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

    public string ElementType { get; set; } = "image";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; }
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListImageItem> CreateInstance(ImageContentActions itemActions)
    {
        return Task.FromResult(new MapElementListImageItem(itemActions, ImageContent.CreateInstance()));
    }
}