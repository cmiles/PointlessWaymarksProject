using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.RssReaderData.Models;

namespace PointlessWaymarks.RssReaderGui.Controls;

[NotifyPropertyChanged]
public partial class FeedItemListListItem
{
    public required Feed DbFeed { get; set; }
    public required FeedItem DbItem { get; set; }
}