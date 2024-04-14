using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

public interface IMapElementListItem : ISelectedTextTracker
{
    MapElementSettings ElementSettings { get; set; }
    string ElementType { get; set; }
    string Title { get; set; }
    Guid? ContentId();
}