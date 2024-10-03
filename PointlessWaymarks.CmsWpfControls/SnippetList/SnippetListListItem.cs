using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.SnippetList;

[NotifyPropertyChanged]
public partial class SnippetListListItem : ISelectedTextTracker
{
    public required Snippet DbEntry { get; set; }
    public CurrentSelectedTextTracker? SelectedTextTracker { get; set; } = new();

    public static SnippetListListItem CreateInstance(Snippet dbItem)
    {
        return new SnippetListListItem { DbEntry = dbItem };
    }
}