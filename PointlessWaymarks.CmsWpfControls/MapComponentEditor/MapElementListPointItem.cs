using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListPointItem : IMapElementListItem
{
    public PointContentDto? DbEntry { get; set; }
    public string SmallImageUrl { get; set; } = string.Empty;

    public Guid? ContentId()
    {
        return DbEntry?.ContentId;
    }

    public string ElementType { get; set; } = "point";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; }
    public string Title { get; set; } = string.Empty;
}