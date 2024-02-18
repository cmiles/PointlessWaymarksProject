using PointlessWaymarks.CmsData.Database.Models;
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

    public string ElementType { get; set; } = "line";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; }
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListLineItem> CreateInstance(LineContentActions itemActions)
    {
        return Task.FromResult(new MapElementListLineItem(itemActions, LineContent.CreateInstance()));
    }
}