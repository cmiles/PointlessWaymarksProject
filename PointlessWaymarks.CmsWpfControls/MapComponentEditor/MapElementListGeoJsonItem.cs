using PointlessWaymarks.CmsData.Database.Models;
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

    public string ElementType { get; set; } = "geojson";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; } = true;
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListGeoJsonItem> CreateInstance(GeoJsonContentActions itemActions)
    {
        return Task.FromResult(new MapElementListGeoJsonItem(itemActions, GeoJsonContent.CreateInstance()));
    }
}