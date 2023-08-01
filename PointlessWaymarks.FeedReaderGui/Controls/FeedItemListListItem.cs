using PointlessWaymarks.FeedReaderData.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
public partial class FeedItemListListItem
{
    public required Feed DbFeed { get; set; }
    public required FeedItem DbItem { get; set; }
}