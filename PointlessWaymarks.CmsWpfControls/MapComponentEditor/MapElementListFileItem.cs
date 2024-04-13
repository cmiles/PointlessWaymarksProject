using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.MapComponentEditor;

[NotifyPropertyChanged]
public partial class MapElementListFileItem : FileListListItem, IMapElementListItem
{
    private protected MapElementListFileItem(FileContentActions itemActions, FileContent dbEntry) : base(itemActions, dbEntry)
    {
    }

    public string ElementType { get; set; } = "file";
    public bool InInitialView { get; set; }
    public bool IsFeaturedElement { get; set; }
    public bool ShowInitialDetails { get; set; }
    public string Title { get; set; } = string.Empty;

    public new static Task<MapElementListFileItem> CreateInstance(FileContentActions itemActions)
    {
        return Task.FromResult(new MapElementListFileItem(itemActions, FileContent.CreateInstance()));
    }
}