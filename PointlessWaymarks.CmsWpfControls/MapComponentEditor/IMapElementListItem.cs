using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor
{
    public interface IMapElementListItem : IContentCommonGuiListItem
    {
        bool InInitialView { get; set; }
        bool IsFeaturedElement { get; set; }
        bool ShowInitialDetails { get; set; }
    }
}