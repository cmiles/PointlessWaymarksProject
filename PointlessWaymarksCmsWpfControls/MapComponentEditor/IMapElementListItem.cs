using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.MapComponentEditor
{
    public interface IMapElementListItem : IContentCommonGuiListItem
    {
        bool InInitialView { get; set; }
        bool IsFeaturedElement { get; set; }
        bool ShowInitialDetails { get; set; }
    }
}