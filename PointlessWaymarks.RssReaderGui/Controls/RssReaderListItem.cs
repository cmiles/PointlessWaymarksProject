using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.RssReaderData.Models;

namespace PointlessWaymarks.RssReaderGui.Controls;

[NotifyPropertyChanged]
public partial class RssReaderListItem
{
    public required RssFeed DbFeed { get; set; }
    public required RssItem DbItem { get; set; }
}