using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListGeoJsonItem : IMapElementListItem
{
    public GeoJsonContent? DbEntry { get; set; }
    public string SmallImageUrl { get; set; } = string.Empty;

    public Guid? ContentId()
    {
        return DbEntry?.ContentId;
    }

    public string ElementType { get; set; } = "geojson";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; } = true;
    public string Title { get; set; } = string.Empty;
}