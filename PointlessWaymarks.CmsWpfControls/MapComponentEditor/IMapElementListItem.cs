namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

public interface IMapElementListItem
{
    bool InInitialView { get; set; }
    bool IsFeaturedElement { get; set; }
    bool ShowInitialDetails { get; set; }
    Guid? ContentId();
}