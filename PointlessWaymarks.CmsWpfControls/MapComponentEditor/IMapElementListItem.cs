namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

public interface IMapElementListItem
{
    string ElementType { get; set; }
    bool InInitialView { get; set; }
    bool IsFeaturedElement { get; set; }
    bool ShowInitialDetails { get; set; }
    string Title { get; set; }
    Guid? ContentId();
}