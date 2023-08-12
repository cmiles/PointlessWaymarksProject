using PointlessWaymarks.FeedReaderData.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
public partial class FeedItemListListItem
{
    public required ReaderFeed DbReaderFeed { get; set; }
    public required ReaderFeedItem DbItem { get; set; }
}