using PointlessWaymarks.FeedReaderData.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
public partial class SavedFeedItemListListItem
{
    public required SavedFeedItem DbItem { get; set; }
    public required ReaderFeed? DbReaderFeed { get; set; }
}