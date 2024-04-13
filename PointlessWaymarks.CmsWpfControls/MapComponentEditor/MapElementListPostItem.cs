using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListPostItem : PostListListItem, IMapElementListItem
{
    private protected MapElementListPostItem(PostContentActions itemActions, PostContent dbEntry) : base(itemActions,
        dbEntry)
    {
    }

    public string ElementType { get; set; } = "post";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; }
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListPostItem> CreateInstance(PostContentActions itemActions)
    {
        return Task.FromResult(new MapElementListPostItem(itemActions, PostContent.CreateInstance()));
    }
}