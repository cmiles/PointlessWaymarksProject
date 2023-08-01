using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.RssReaderData.Models;

namespace PointlessWaymarks.RssReaderGui.Controls;

[NotifyPropertyChanged]
public partial class FeedListListItem
{
    public int UnreadItemsCount { get; set; }
    public int ItemsCount { get; set; }
    public required Feed DbFeed { get; set; }
}