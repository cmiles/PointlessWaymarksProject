using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
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

    public string ElementType { get; set; } = "point";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; }
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListPointItem> CreateInstance(PointContentActions itemActions)
    {
        var newPoint = PointContent.CreateInstance();
        var newPointDto = Db.PointContentDtoFromPointContentAndDetails(newPoint, new List<PointDetail>(), null);

        return Task.FromResult(new MapElementListPointItem(itemActions, newPointDto));
    }
}